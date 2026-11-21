using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rage;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class AmbientPopulation
    {
        private const int PreferredInitialPatients = 12;
        private static readonly string[] PatientModels =
        {
            "a_m_y_business_02", "a_f_y_hipster_02", "a_m_m_bevhills_02",
            "a_f_m_bevhills_01", "a_m_y_hipster_01", "a_f_y_business_02"
        };

        private readonly HospitalPath path;
        private readonly PopulationSettings settings;
        private readonly Point[] staffAnchors;
        private readonly Point[] patientAnchors;
        private readonly Point[] staffStarts;
        private readonly Point[] patientStarts;
        private readonly int maxDoctors;
        private readonly Random random = new Random();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<AmbientPatrol> patrols = new List<AmbientPatrol>();
        private long nextArrival;
        private long nextDeparture;
        private int lastPatientModel = -1;

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

            nextArrival = clock.ElapsedMilliseconds + random.Next(6000, 10001);
            nextDeparture = clock.ElapsedMilliseconds + random.Next(18000, 32001);
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
            long now = clock.ElapsedMilliseconds;
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
            nextArrival = clock.ElapsedMilliseconds + random.Next(6000, 10001);
        }

        private AmbientRole PickWorkerRole(int workers)
        {
            int doctors = patrols.Count(patrol => patrol.Role == AmbientRole.Doctor);
            int nurses = workers - doctors;
            return doctors < maxDoctors && (nurses >= settings.MaxWorkers - maxDoctors || doctors * 3L <= nurses)
                ? AmbientRole.Doctor : AmbientRole.Nurse;
        }

        private void StartDeparture()
        {
            if (clock.ElapsedMilliseconds < nextDeparture || patrols.Count(patrol => patrol.IsLeaving) >= 2 ||
                patrols.Count(patrol => patrol.CountsInside) <= PopulationSettings.MinimumInsideForDeparture)
            {
                return;
            }

            AmbientPatrol[] available = patrols.Where(patrol => patrol.CanLeave).ToArray();
            if (available.Length > 0 && available[random.Next(available.Length)].TryLeave())
            {
                nextDeparture = clock.ElapsedMilliseconds + random.Next(18000, 32001);
            }
        }

        private bool Spawn(AmbientRole role, Point start, bool entering = false)
        {
            string model;
            if (role == AmbientRole.Patient)
            {
                int index = random.Next(PatientModels.Length);
                if (index == lastPatientModel)
                {
                    index = (index + random.Next(1, PatientModels.Length)) % PatientModels.Length;
                }
                lastPatientModel = index;
                model = PatientModels[index];
            }
            else
            {
                model = role == AmbientRole.Doctor ? "s_m_m_doctor_01" : "s_f_y_scrubs_01";
            }

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
