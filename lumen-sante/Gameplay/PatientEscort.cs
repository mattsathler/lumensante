using System;
using System.Linq;
using Rage;
using Rage.Native;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class PatientEscort
    {
        private readonly AmbientPatrol nurse;
        private readonly Ped patient;
        private readonly HospitalPath path;
        private readonly Point pickup;
        private readonly Point destination;
        private readonly NpcRoute walker = new NpcRoute();
        private readonly NpcRoute patientWalk = new NpcRoute();
        private Point[] escortRoute;
        private bool patientStarted;
        private bool nurseWaiting;
        private bool collected;
        private bool released;
        private readonly Point nurseOrigin;
        internal Point RetryPickup => collected ? walker.LastGraphPoint ?? pickup : pickup;

        internal PatientEscort(HospitalPath path, AmbientPatrol nurse, Ped patient, Point pickup, Point destination)
        {
            this.path = path;
            this.nurse = nurse;
            this.patient = patient;
            this.pickup = pickup;
            this.destination = destination;
            Point origin = nurse.AssignToExam();
            nurseOrigin = origin;
            try
            {
                Point[] receptionRoute = path.FindRoute(origin, HospitalStaffRoutes.Reception);
                Point[] pickupRoute = path.FindRoute(HospitalStaffRoutes.Reception, pickup);
                if (receptionRoute == null || pickupRoute == null || path.FindRoute(pickup, destination) == null)
                    throw new InvalidOperationException("Sem rota para buscar o paciente na recepção.");
                Walk(origin, receptionRoute.Concat(pickupRoute).ToArray());
            }
            catch
            {
                Release();
                throw;
            }
        }

        internal bool Update()
        {
            if (!nurse.Actor.IsAlive || !patient.Exists() || !patient.IsAlive)
                throw new InvalidOperationException("Acompanhamento interrompido: enfermeiro ou paciente indisponível.");

            if (!collected)
            {
                if (walker.IsWalking && !walker.Update(nurse.Actor.Actor)) return false;
                patient.Tasks.Clear();
                patient.IsPositionFrozen = false;
                NativeFunction.Natives.SET_ENTITY_COLLISION(patient, true, true);
                NpcWalking.TeleportToWaypoint(patient, pickup.Position);
                collected = true;
                escortRoute = path.FindRoute(pickup, destination);
                Walk(pickup, escortRoute);
                return false;
            }

            bool nurseArrived = !walker.IsWalking || walker.Update(nurse.Actor.Actor);
            if (!patientStarted && (nurseArrived || (nurse.Actor.Actor.Position - pickup.Position).LengthSquared() > 1f))
            {
                patientStarted = true;
                if (escortRoute.Length > 0) patientWalk.Start(patient, pickup, escortRoute, teleportOnTimeout: false);
            }
            NativeFunction.Natives.SET_ENTITY_NO_COLLISION_ENTITY(patient, nurse.Actor.Actor, true);
            bool patientArrived = patientStarted && (!patientWalk.IsWalking || patientWalk.Update(patient));
            if (nurseArrived && !nurseWaiting)
            {
                nurse.Actor.Actor.Tasks.StandStill(-1);
                nurseWaiting = true;
            }
            if (!nurseArrived || !patientArrived) return false;
            patient.Tasks.Clear();
            Release();
            return true;
        }

        private void Walk(Point origin, Point[] route)
        {
            if (route.Length == 0) return;
            walker.Start(nurse.Actor.Actor, origin, route, teleportOnTimeout: false);
        }

        internal void Release()
        {
            if (released) return;
            released = true;
            if (nurse.Actor.IsAlive)
            {
                nurse.Actor.Actor.Tasks.Clear();
                nurse.ResumeAfterExam(walker.LastGraphPoint ?? nurseOrigin);
            }
            walker.Clear();
            patientWalk.Clear();
            if (collected && patient.Exists() && patient.IsAlive) patient.Tasks.Clear();
        }
    }
}
