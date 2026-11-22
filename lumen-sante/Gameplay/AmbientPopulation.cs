using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class AmbientPopulation
    {
        private const int PreferredInitialPatients = 12;

        private readonly HospitalPath path;
        private readonly PopulationSettings settings;
        private readonly Point[] staffAnchors;
        private readonly Point[] patientAnchors;
        private readonly Point[] staffStarts;
        private readonly Point[] patientStarts;
        private readonly int maxDoctors;
        private readonly Random random = new Random();
        private readonly List<AmbientPatrol> patrols = new List<AmbientPatrol>();
        private long nextArrival;
        private long nextDeparture;


        internal AmbientPopulation(HospitalPath path, PopulationSettings settings, Point[] staffAnchors,
            Point[] patientAnchors, Point[] staffStarts, Point[] patientStarts)
        {
            this.path = path;
            this.settings = settings;
            this.staffAnchors = staffAnchors;
            this.patientAnchors = patientAnchors;
            this.staffStarts = staffStarts;
            this.patientStarts = patientStarts;
            maxDoctors = (int)Math.Ceiling(settings.MaxWorkers / 4d);
        }

        internal IEnumerable<HospitalReceptionist> Actors => patrols.Select(patrol => patrol.Actor);

        internal AmbientPatrol FindNurse(Point destination) => patrols
            .Where(patrol => patrol.CanTakeExam)
            .OrderBy(patrol => (patrol.Actor.Actor.Position - destination.Position).LengthSquared())
            .FirstOrDefault();

        internal void Populate()
        {
            int initialPatients = Math.Min(settings.MaxAmbientNpcPatients,
                Math.Max(PreferredInitialPatients, PopulationSettings.MinimumInitialPopulation - settings.MaxWorkers));
            int initialWorkers = PopulationSettings.MinimumInitialPopulation - initialPatients;
            int initialDoctors = Math.Max(initialWorkers - (settings.MaxWorkers - maxDoctors),
                Math.Min(maxDoctors, initialWorkers / 4));
            int workersSpawned = 0;
            foreach (Point start in staffStarts)
            {
                if (workersSpawned == initialWorkers)
                {
                    break;
                }
                if (Spawn(workersSpawned < initialDoctors ? AmbientRole.Doctor : AmbientRole.Nurse, start))
                {
                    workersSpawned++;
                }
            }
            int patientsSpawned = 0;
            foreach (Point start in patientStarts)
            {
                if (patientsSpawned == initialPatients)
                {
                    break;
                }
                if (Spawn(AmbientRole.Patient, start))
                {
                    patientsSpawned++;
                }
            }

            ScheduleArrival();
            nextDeparture = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(45, 91));
        }

        internal void Update(bool allowTurnover)
        {
            for (int index = patrols.Count - 1; index >= 0; index--)
            {
                if (!patrols[index].Actor.IsAlive)
                {
                    patrols[index].Clear();
                    patrols.RemoveAt(index);
                }
            }

            if (allowTurnover)
            {
                StartDeparture();
                StartArrival();
            }

            foreach (AmbientPatrol patrol in patrols)
            {
                try
                {
                    patrol.Update();
                }
                catch (InvalidOperationException error)
                {
                    patrol.Clear();
                    Game.LogTrivial("[Lumen Santé] Falha na circulação de ambiente: " + error.Message);
                }
            }
        }

        private void StartArrival()
        {
            long now = GameClock.NowMilliseconds;
            if (now < nextArrival || patrols.Count(patrol => patrol.IsEntering) >= 2)
            {
                return;
            }

            int patients = patrols.Count(patrol => patrol.Role == AmbientRole.Patient);
            int workers = patrols.Count - patients;
            bool patientSlot = patients < settings.MaxAmbientNpcPatients;
            bool workerSlot = workers < settings.MaxWorkers;
            if (!patientSlot && !workerSlot)
            {
                return;
            }

            AmbientRole role = patientSlot && (!workerSlot || random.Next(2) == 0)
                ? AmbientRole.Patient : PickWorkerRole(workers);
            Spawn(role, HospitalStaffRoutes.PatientSpawn, entering: true);
            ScheduleArrival();
        }

        internal void ScheduleArrival() =>
            nextArrival = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(15, 31));

        private AmbientRole PickWorkerRole(int workers)
        {
            int doctors = patrols.Count(patrol => patrol.Role == AmbientRole.Doctor);
            int nurses = workers - doctors;
            return doctors < maxDoctors && (nurses >= settings.MaxWorkers - maxDoctors || doctors * 3L <= nurses)
                ? AmbientRole.Doctor : AmbientRole.Nurse;
        }

        private void StartDeparture()
        {
            if (GameClock.NowMilliseconds < nextDeparture || patrols.Count(patrol => patrol.IsLeaving) >= 2 ||
                patrols.Count(patrol => patrol.CountsInside) <= PopulationSettings.MinimumInsideForDeparture)
            {
                return;
            }

            AmbientPatrol[] available = patrols.Where(patrol => patrol.CanLeave).ToArray();
            if (available.Length > 0 && available[random.Next(available.Length)].TryLeave())
            {
                nextDeparture = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(45, 91));
            }
        }

        private bool Spawn(AmbientRole role, Point start, bool entering = false)
        {
            string model = role == AmbientRole.Doctor ? "s_m_m_doctor_01" : "s_f_y_scrubs_01";

            var patrol = new AmbientPatrol(role, model, path, start,
                role == AmbientRole.Patient ? patientAnchors : staffAnchors, random);
            patrol.Start(entering);
            if (!patrol.Actor.IsAlive)
            {
                patrol.Clear();
                return false;
            }

            patrols.Add(patrol);
            return true;
        }

        internal void Clear()
        {
            foreach (AmbientPatrol patrol in patrols)
            {
                patrol.Clear();
            }
            patrols.Clear();
        }
    }
}
