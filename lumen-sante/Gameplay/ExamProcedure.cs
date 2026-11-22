using System;
using System.Linq;
using Rage;
using Rage.Native;
using lumen_sante.Clinical;
using Point = lumen_sante.Gameplay.HospitalPath.Point;
using Station = lumen_sante.Gameplay.HospitalExamRoutes.Station;

namespace lumen_sante.Gameplay
{
    internal sealed class ExamProcedure
    {
        internal enum Result { Running, Completed, Cancelled }
        private enum Stage { ToBed, AtBed, ToEquipment, Exam, Returning, ReturningCancelled, Cancelled }

        private readonly HospitalPath path;
        private readonly AmbientPatrol nurse;
        private readonly Ped patient;
        private readonly Point roomCenter;
        private readonly Vector3 bed;
        private readonly float bedHeading;
        private int duration;

        private readonly Station staffStation;
        private readonly Station patientStation;
        private readonly NpcRoute staffWalk = new NpcRoute();
        private readonly NpcRoute patientWalk = new NpcRoute();
        private Point nurseOrigin;
        private Stage stage;
        private long resumeAt;
        private bool nurseReleased;
        private bool nurseAtStation;
        private bool patientAtStation;

        internal ExamProcedure(HospitalStaffRoutes routes, AmbientPatrol nurse, Ped patient, Point roomCenter,
            Vector3 bed, float bedHeading, ExamOrder order)
        {
            path = routes.Path;
            this.nurse = nurse;
            this.patient = patient;
            this.roomCenter = roomCenter;
            this.bed = bed;
            this.bedHeading = bedHeading;
            duration = order.DurationMilliseconds;

            var stations = routes.ExamRoutes.For(order.Kind);
            staffStation = stations.Staff;
            patientStation = stations.Patient;
            nurseOrigin = nurse.AssignToExam();
            try
            {
                StartWalk(staffWalk, nurse.Actor.Actor, nurseOrigin, roomCenter);
            }
            catch
            {
                ReleaseNurse();
                throw;
            }
        }

        internal bool PatientAway { get; private set; }
        internal bool CanGroupOrders => stage == Stage.ToBed || stage == Stage.AtBed || stage == Stage.ToEquipment;

        internal void IncludeDuration(int milliseconds)
        {
            if (!CanGroupOrders) throw new InvalidOperationException("A realização do exame já começou.");
            duration = Math.Max(duration, milliseconds);
        }
        internal ExamStatus OrderStatus => stage == Stage.Exam
            ? ExamStatus.InProgress
            : stage == Stage.Returning ? ExamStatus.Processing : ExamStatus.Pending;

        internal Result Update()
        {
            if (!patient.Exists() || !patient.IsAlive)
            {
                ReleaseNurse();
                return Result.Cancelled;
            }

            if (!nurse.Actor.IsAlive && stage != Stage.ReturningCancelled && stage != Stage.Cancelled)
            {
                Cancel();
            }

            switch (stage)
            {
                case Stage.ToBed:
                    if (Arrived(staffWalk, nurse.Actor.Actor))
                    {
                        nurse.Actor.Actor.Tasks.StandStill(2000);
                        resumeAt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(2000);
                        stage = Stage.AtBed;
                    }
                    break;
                case Stage.AtBed:
                    if (GameClock.NowMilliseconds >= resumeAt)
                    {
                        LeaveBed();
                    }
                    break;
                case Stage.ToEquipment:
                    bool nurseArrived = Arrived(staffWalk, nurse.Actor.Actor);
                    bool patientArrived = !PatientAway || Arrived(patientWalk, patient);
                    if (nurseArrived && patientArrived)
                    {
                        StartExam();
                    }
                    break;
                case Stage.Exam:
                    if (GameClock.NowMilliseconds >= resumeAt)
                    {
                        if (staffStation == null)
                        {
                            ReleaseNurse();
                            return Result.Completed;
                        }
                        StartReturn();
                    }
                    break;
                case Stage.Returning:
                    bool nurseReturned = Arrived(staffWalk, nurse.Actor.Actor);
                    bool patientReturned = !PatientAway || Arrived(patientWalk, patient);
                    if (nurseReturned && patientReturned)
                    {
                        RestoreBed();
                        ReleaseNurse();
                        return Result.Completed;
                    }
                    break;
                case Stage.ReturningCancelled:
                    if (Arrived(patientWalk, patient))
                    {
                        RestoreBed();
                        return Result.Cancelled;
                    }
                    break;
                case Stage.Cancelled:
                    return Result.Cancelled;
            }
            return Result.Running;
        }

        private void LeaveBed()
        {
            if (staffStation == null)
            {
                nurse.Actor.Actor.Tasks.StandStill(-1);
                resumeAt = GameClock.NowMilliseconds + duration;
                stage = Stage.Exam;
                return;
            }

            StartWalk(staffWalk, nurse.Actor.Actor, roomCenter, staffStation.Approach);
            if (patientStation != null)
            {
                StandUp(patient);
                PatientAway = true;
                StartWalk(patientWalk, patient, roomCenter, patientStation.Approach, visitOrigin: true);
            }
            stage = Stage.ToEquipment;
        }

        private void StartExam()
        {
            Pose(nurse.Actor.Actor, staffStation.Position, staffStation.Heading,
                staffStation.Dictionary, staffStation.Clip, staffStation.Elevated);
            nurseAtStation = true;
            if (PatientAway)
            {
                Pose(patient, patientStation.Position, patientStation.Heading,
                    patientStation.Dictionary, patientStation.Clip, freeze: true);
                patientAtStation = true;
            }
            resumeAt = GameClock.NowMilliseconds + duration;
            stage = Stage.Exam;
        }

        private void StartReturn()
        {
            MoveNurseOffStation();
            StartWalk(staffWalk, nurse.Actor.Actor, staffStation.Approach, roomCenter);
            if (PatientAway)
            {
                ReturnPatient();
            }
            stage = Stage.Returning;
        }

        internal void Cancel()
        {
            ReleaseNurse();
            if (PatientAway && patient.Exists() && patient.IsAlive)
            {
                ReturnPatient();
                stage = Stage.ReturningCancelled;
            }
            else
            {
                stage = Stage.Cancelled;
            }
        }

        private void ReturnPatient()
        {
            Point origin = patientWalk.LastGraphPoint ?? roomCenter;
            if (patientAtStation)
            {
                StandUp(patient, patientStation.Approach);
                patientAtStation = false;
            }
            StartWalk(patientWalk, patient, origin, roomCenter, visitOrigin: true);
        }

        private void RestoreBed()
        {
            if (PatientAway)
            {
                var pose = BedAnimations.Lounger;
                Pose(patient, bed, bedHeading, pose.Dictionary, pose.Clip, freeze: true);
                PatientAway = false;
            }
        }

        internal void ReleaseNurse()
        {
            if (nurseReleased)
            {
                return;
            }
            nurseReleased = true;
            if (nurse.Actor.IsAlive)
            {
                MoveNurseOffStation();
                Ped ped = nurse.Actor.Actor;
                ped.Tasks.Clear();
                nurse.ResumeAfterExam(staffWalk.LastGraphPoint ?? nurseOrigin);
            }
        }

        private void MoveNurseOffStation()
        {
            StandUp(nurse.Actor.Actor, nurseAtStation && staffStation.Elevated ? staffStation.Approach : null);
            nurseAtStation = false;
        }

        private void StartWalk(NpcRoute walker, Ped actor, Point origin, Point destination, bool visitOrigin = false)
        {
            Point[] route = path.FindRoute(origin, destination);
            if (route == null)
            {
                throw new InvalidOperationException("Sem rota de exame entre " + origin.Name + " e " + destination.Name + ".");
            }

            walker.Clear();
            if (visitOrigin)
            {
                route = new[] { origin }.Concat(route).ToArray();
            }
            if (route.Length > 0)
            {
                walker.Start(actor, origin, route);
            }
        }

        private static bool Arrived(NpcRoute walker, Ped actor) => !walker.IsWalking || walker.Update(actor);

        private static void StandUp(Ped actor, Point approach = null)
        {
            actor.Tasks.Clear();
            actor.IsPositionFrozen = false;
            if (approach != null)
            {
                NpcWalking.TeleportToWaypoint(actor, approach.Position);
            }
            NativeFunction.Natives.SET_ENTITY_COLLISION(actor, true, true);
        }

        private static void Pose(Ped actor, Vector3 position, float heading, string dictionary, string clip, bool freeze)
        {
            HospitalReceptionist.LoadAnimation(dictionary);
            actor.Tasks.Clear();
            if (freeze)
            {
                NativeFunction.Natives.SET_ENTITY_COLLISION(actor, false, true);
            }
            HospitalReceptionist.PlaceAt(actor, position, heading, dictionary, clip);
            actor.IsPositionFrozen = freeze;
        }
    }
}
