using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class WaitingRoom
    {
        private const int MinimumOccupants = 8;
        private const int MaximumOccupants = 12;
        private readonly Random random = new Random();
        private readonly HospitalPath path;
        private readonly NpcRoute walker = new NpcRoute();
        private readonly Point[] seatApproaches;
        private long nextArrival;
        private long resumeArrival;
        private int populationTarget;
        private Ped arrivingPatient;
        private ArrivalPhase phase;
        private int arrivalSeatIndex;
        private bool priorityArrival;

        private enum ArrivalPhase { ToDoor, AtDoor, ToIdle, AtIdle, ToSeat }

        internal static readonly (string Name, string Dictionary, string Clip) ElbowOnKnee =
            ("Cotovelo no joelho", "amb@prop_human_seat_chair@male@left_elbow_on_knee@base", "base");
        internal static readonly (string Name, string Dictionary, string Clip) FootForward =
            ("Pé à frente", "amb@prop_human_seat_chair@male@right_foot_out@base", "base");

        internal static readonly (string Name, string Dictionary, string Clip)[] SeatedPoses =
        {
            ("Espera", HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip),
            ElbowOnKnee,
            FootForward
        };

        private static readonly (Vector3 Position, float Heading)[] Seats =
        {
            (new Vector3(352.004f, -1395.091f, 31.955f), 233.3f),
            (new Vector3(351.604f, -1395.442f, 31.955f), 233.3f),
            (new Vector3(350.055f, -1397.393f, 31.955f), 233.3f),
            (new Vector3(349.355f, -1398.244f, 31.955f), 233.3f),
            (new Vector3(349.658f, -1397.749f, 31.955f), 233.3f),
            (new Vector3(346.541f, -1396.652f, 31.947f), 232.7f),
            (new Vector3(346.941f, -1396.201f, 31.947f), 232.7f),
            (new Vector3(347.241f, -1395.701f, 31.947f), 232.7f),
            (new Vector3(347.741f, -1395.150f, 31.947f), 232.7f),
            (new Vector3(348.091f, -1394.750f, 31.947f), 232.7f),
            (new Vector3(348.391f, -1394.249f, 31.947f), 232.7f),
            (new Vector3(349.841f, -1392.748f, 31.947f), 232.7f),
            (new Vector3(350.140f, -1392.247f, 31.947f), 232.7f),
            (new Vector3(350.440f, -1391.847f, 31.947f), 232.7f)
        };


        private readonly Dictionary<int, HospitalReceptionist> occupants = new Dictionary<int, HospitalReceptionist>();
        private readonly List<int> queue = new List<int>();
        internal IEnumerable<HospitalReceptionist> Occupants => occupants.Values;
        internal IEnumerable<Ped> Patients => occupants.Values.Where(actor => actor.IsAlive).Select(actor => actor.Actor)
            .Concat(arrivingPatient != null && arrivingPatient.Exists() && arrivingPatient.IsAlive
                ? new[] { arrivingPatient } : Array.Empty<Ped>());
        internal int WaitingPatientCount => queue.Count(index => occupants[index].IsAlive);
        internal event Action<Ped> PatientReady;
        internal bool CanReceiveReturningPatient => arrivingPatient == null && Enumerable.Range(0, Seats.Length)
            .Any(index => !occupants.TryGetValue(index, out HospitalReceptionist actor) || !actor.IsAlive);

        internal void ReceiveReturningPatient(Ped patient)
        {
            if (!CanReceiveReturningPatient) throw new InvalidOperationException("Recepção sem vaga para retorno.");
            arrivingPatient = patient;
            priorityArrival = false;
            arrivingPatient.IsPersistent = true;
            arrivingPatient.BlockPermanentEvents = true;
            StartWalk(ArrivalPhase.ToDoor, HospitalStaffRoutes.PatientSpawn, HospitalStaffRoutes.HospitalDoor);
        }

        internal bool ReceiveFromConsultation(Ped patient, Point origin)
        {
            if (!CanReceiveReturningPatient) return false;
            Point[] route = path.FindRoute(origin, HospitalStaffRoutes.PatientArrivalIdle);
            if (route == null || route.Length == 0) return false;
            arrivingPatient = patient;
            priorityArrival = true;
            patient.IsPositionFrozen = false;
            Rage.Native.NativeFunction.Natives.SET_ENTITY_COLLISION(patient, true, true);
            phase = ArrivalPhase.ToIdle;
            walker.Start(patient, origin, new[] { origin }.Concat(route).ToArray());
            return true;
        }

        internal bool ReceiveAtReception(Ped patient)
        {
            if (!CanReceiveReturningPatient) return false;
            arrivingPatient = patient;
            priorityArrival = true;
            WaitAtReception();
            return true;
        }

        internal WaitingRoom(HospitalStaffRoutes routes)
        {
            path = routes.Path;
            seatApproaches = Seats.Select((seat, index) => routes.AddWaitingBench(index, seat.Position, seat.Heading)).ToArray();
        }

        private int PickFreeSeat(Random selection)
        {
            int[] freeSeats = Enumerable.Range(0, Seats.Length)
                .Where(index => !occupants.TryGetValue(index, out HospitalReceptionist actor) || !actor.IsAlive)
                .ToArray();
            return freeSeats.Length == 0 ? -1 : freeSeats[selection.Next(freeSeats.Length)];
        }

        internal bool IsWaiting(Ped patient) => queue.Any(index => occupants[index].IsAlive && occupants[index].Actor == patient);

        internal Ped FirstWaiting(Func<Ped, bool> accept) => queue
            .Where(index => occupants[index].IsAlive).Select(index => occupants[index].Actor).FirstOrDefault(accept);

        internal void RemovePatient(Ped patient)
        {
            if (patient == null) return;
            if (arrivingPatient == patient)
            {
                DiscardArrival();
                return;
            }
            int index = queue.FindIndex(seat => occupants[seat].Actor == patient);
            if (index < 0) return;
            int seatIndex = queue[index];
            occupants[seatIndex].Clear();
            occupants.Remove(seatIndex);
            queue.RemoveAt(index);
        }

        internal bool TryCallPatient(out Ped patient, out Point approach, Ped requestedPatient = null)
        {
            int queueIndex = queue.FindIndex(index => occupants[index].IsAlive &&
                (requestedPatient == null || occupants[index].Actor == requestedPatient));
            if (queueIndex < 0)
            {
                patient = null;
                approach = null;
                return false;
            }

            int seatIndex = queue[queueIndex];
            patient = occupants[seatIndex].Release();
            approach = seatApproaches[seatIndex];
            queue.RemoveAt(queueIndex);
            occupants.Remove(seatIndex);
            return true;
        }

        internal void Populate()
        {
            populationTarget = Math.Min(Seats.Length, random.Next(MinimumOccupants, MaximumOccupants + 1));
            int count = random.Next(3, 5);
            for (int index = 0; index < count; index++)
            {
                OccupySeat(PickFreeSeat(random));
            }

            ScheduleArrival();
        }

        internal void Update(bool canStartArrival)
        {
            if (!canStartArrival) return;
            try
            {
                if (arrivingPatient == null)
                {
                    if (canStartArrival && GameClock.NowMilliseconds >= nextArrival &&
                        occupants.Values.Count(actor => actor.IsAlive) < populationTarget)
                    {
                        Point spawn = HospitalStaffRoutes.PatientSpawn;
                        arrivingPatient = PatientAppearance.Create(random, spawn.Position, spawn.Heading);
                        if (!arrivingPatient.Exists())
                        {
                            throw new InvalidOperationException("NPC indisponível.");
                        }

                        arrivingPatient.IsPersistent = true;
                        arrivingPatient.BlockPermanentEvents = true;
                        StartWalk(ArrivalPhase.ToDoor, spawn, HospitalStaffRoutes.HospitalDoor);
                    }
                    return;
                }

                if (!arrivingPatient.Exists() || !arrivingPatient.IsAlive)
                {
                    DiscardArrival();
                    return;
                }

                if (walker.IsWalking)
                {
                    if (walker.Update(arrivingPatient))
                    {
                        FinishWalk();
                    }
                    return;
                }

                if (GameClock.NowMilliseconds < resumeArrival)
                {
                    return;
                }

                Point idle = HospitalStaffRoutes.PatientArrivalIdle;
                if (phase == ArrivalPhase.AtDoor)
                {
                    StartWalk(ArrivalPhase.ToIdle, HospitalStaffRoutes.HospitalDoor, idle);
                }
                else if (phase == ArrivalPhase.AtIdle)
                {
                    arrivalSeatIndex = PickFreeSeat(random);
                    if (arrivalSeatIndex >= 0)
                    {
                        StartWalk(ArrivalPhase.ToSeat, idle, seatApproaches[arrivalSeatIndex]);
                    }
                    else
                    {
                        resumeArrival = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(1000);
                    }
                }
            }
            catch (Exception error)
            {
                DiscardArrival();
                Game.LogTrivial("[Lumen Santé] Falha na chegada de um paciente da sala de espera: " + error.Message);
            }
        }

        private void FinishWalk()
        {
            switch (phase)
            {
                case ArrivalPhase.ToDoor:
                    arrivingPatient.Tasks.Clear();
                    arrivingPatient.Heading = HospitalStaffRoutes.HospitalDoor.Heading;
                    phase = ArrivalPhase.AtDoor;
                    resumeArrival = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(1000);
                    break;
                case ArrivalPhase.ToIdle:
                    WaitAtReception();
                    break;
                case ArrivalPhase.ToSeat:
                    SeatArrivingPatient();
                    break;
            }
        }

        private void WaitAtReception()
        {
            Point idle = HospitalStaffRoutes.PatientArrivalIdle;
            var animation = idle.Animations[random.Next(idle.Animations.Length)];
            arrivingPatient.Tasks.Clear();
            try
            {
                HospitalReceptionist.LoadAnimation(animation.Dictionary);
                HospitalReceptionist.PlaceAt(arrivingPatient, idle.Position, idle.Heading,
                    animation.Dictionary, animation.Clip);
            }
            catch (InvalidOperationException error)
            {
                Game.LogTrivial("[Lumen Santé] Falha na animação de espera inicial: " + error.Message);
                arrivingPatient.Tasks.StandStill(7500);
            }

            phase = ArrivalPhase.AtIdle;
            resumeArrival = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(random.Next(4000, 7501));
        }

        private void SeatArrivingPatient()
        {
            Ped patient = arrivingPatient;
            OccupySeat(arrivalSeatIndex, patient);
            arrivingPatient = null;
            ScheduleArrival();
            if (IsWaiting(patient)) PatientReady?.Invoke(patient);
        }

        private void OccupySeat(int seatIndex, Ped pedestrian = null)
        {
            if (occupants.TryGetValue(seatIndex, out HospitalReceptionist previousOccupant))
            {
                previousOccupant.Clear();
                occupants.Remove(seatIndex);
                queue.Remove(seatIndex);
            }

            var pose = SeatedPoses[random.Next(SeatedPoses.Length)];
            var occupant = new HospitalReceptionist(pose.Dictionary, pose.Clip);
            var seat = Seats[seatIndex];
            try
            {
                HospitalReceptionist.LoadAnimation(pose.Dictionary);
                pedestrian = pedestrian ?? PatientAppearance.Create(random, seat.Position, seat.Heading);
                occupant.Seat(pedestrian, seat.Position, seat.Heading);
                occupants[seatIndex] = occupant;
                if (pedestrian == arrivingPatient && priorityArrival) queue.Insert(0, seatIndex);
                else queue.Add(seatIndex);
            }
            catch (Exception error)
            {
                if (pedestrian != null && pedestrian.Exists())
                {
                    pedestrian.Delete();
                }
                occupant.Clear();
                Game.LogTrivial("[Lumen Santé] Falha ao ocupar o assento da recepção " +
                    (seatIndex + 1) + ": " + error.Message);
            }
        }

        private void StartWalk(ArrivalPhase nextPhase, Point from, Point to)
        {
            Point[] route = path.FindRoute(from, to);
            if (route == null || route.Length == 0)
            {
                throw new InvalidOperationException("Sem rota de " + from.Name + " até " + to.Name + ".");
            }

            phase = nextPhase;
            walker.Start(arrivingPatient, from, route);
        }

        internal void ScheduleArrival()
        {
            nextArrival = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(20, 41));
        }

        private void DiscardArrival()
        {
            walker.Clear();
            if (arrivingPatient != null && arrivingPatient.Exists())
            {
                arrivingPatient.Delete();
            }
            arrivingPatient = null;
            ScheduleArrival();
        }

        internal void Clear()
        {
            DiscardArrival();
            foreach (HospitalReceptionist occupant in occupants.Values)
            {
                occupant.Clear();
            }

            occupants.Clear();
            queue.Clear();
        }
    }
}
