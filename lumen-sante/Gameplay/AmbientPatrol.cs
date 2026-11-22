using System;
using System.Linq;
using Rage;
using Rage.Native;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal enum AmbientRole { Patient, Nurse, Doctor }

    internal sealed class AmbientPatrol
    {
        private readonly HospitalPath path;
        private readonly Point[] anchors;
        private readonly Random random;
        private readonly NpcRoute walker = new NpcRoute();
        private Point currentStop;
        private Point reservedAnchor;
        private Point destination;
        private long nextAction;
        private long departureEligibleAt;
        private bool seated;
        private bool assignedToExam;
        private Travel travel;
        private (string Dictionary, string Clip) lastAnimation;

        private enum Travel { Inside, EnteringToDoor, EnteringToReception, LeavingToDoor, LeavingToOutside }

        internal AmbientPatrol(AmbientRole role, string model, HospitalPath path, Point start, Point[] anchors, Random random, string name = null, string staffId = null)
        {
            Role = role;
            Name = name;
            StaffId = staffId;
            Actor = new HospitalReceptionist(modelName: model, employeeName: name);
            this.path = path;
            this.anchors = anchors;
            this.random = random;
            currentStop = start;
        }

        internal HospitalReceptionist Actor { get; }
        internal AmbientRole Role { get; }
        internal string Name { get; }
        internal string StaffId { get; }
        internal bool CountsInside => Actor.IsAlive && travel == Travel.Inside;
        internal bool IsEntering => travel == Travel.EnteringToDoor || travel == Travel.EnteringToReception;
        internal bool IsLeaving => travel == Travel.LeavingToDoor || travel == Travel.LeavingToOutside;
        internal bool CanLeave => CountsInside && !walker.IsWalking &&
            !assignedToExam && GameClock.NowMilliseconds >= Math.Max(nextAction, departureEligibleAt);
        internal bool CanTakeExam => Role == AmbientRole.Nurse && CountsInside && !assignedToExam && !walker.IsWalking;
        internal bool CanTakeDoctorWork => Role == AmbientRole.Doctor && CountsInside && !assignedToExam && !walker.IsWalking;

        internal Point AssignToExam()
        {
            LeaveStop(Actor.Actor);
            assignedToExam = true;
            return currentStop;
        }

        internal void ResumeAfterExam(Point point)
        {
            currentStop = point;
            assignedToExam = false;
            nextAction = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(2000);
        }

        internal void Start(bool entering = false)
        {
            try
            {
                if (Role == AmbientRole.Patient)
                {
                    Actor.CreateStanding(PatientAppearance.Create(random, currentStop.Position, currentStop.Heading));
                }
                else
                {
                    Actor.CreateStanding(currentStop.Position, currentStop.Heading);
                }
                if (Role != AmbientRole.Patient)
                {
                    NativeFunction.Natives.SET_PED_RANDOM_COMPONENT_VARIATION(Actor.Actor, false);
                }

                if (entering)
                {
                    FollowPortal(HospitalStaffRoutes.HospitalDoor, Travel.EnteringToDoor);
                    return;
                }

                SetDepartureTime();
                if (currentStop.IsAnchor && path.TryReserveAnchor(currentStop))
                {
                    reservedAnchor = currentStop;
                    RestAtAnchor(Actor.Actor);
                }
                else
                {
                    nextAction = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(random.Next(12000, 36001));
                }
            }
            catch (Exception error)
            {
                ReleaseAnchor();
                Actor.Clear();
                Game.LogTrivial("[Lumen Santé] Falha ao criar NPC de ambiente em " + currentStop.Name + ": " + error.Message);
            }
        }

        internal bool TryLeave()
        {
            if (!CanLeave)
            {
                return false;
            }

            FollowPortal(HospitalStaffRoutes.HospitalDoor, Travel.LeavingToDoor);
            return true;
        }

        internal void Update()
        {
            if (!Actor.IsAlive)
            {
                walker.Clear();
                LeaveStop(Actor.Actor);
                return;
            }

            if (assignedToExam)
            {
                return;
            }

            Ped ped = Actor.Actor;
            long now = GameClock.NowMilliseconds;
            if (!walker.IsWalking)
            {
                if (now >= nextAction)
                {
                    if (travel == Travel.EnteringToDoor)
                    {
                        FollowPortal(HospitalStaffRoutes.Reception, Travel.EnteringToReception);
                    }
                    else if (travel == Travel.LeavingToDoor)
                    {
                        FollowPortal(HospitalStaffRoutes.PatientSpawn, Travel.LeavingToOutside);
                    }
                    else
                    {
                        FindNextDestination(ped);
                    }
                }
                return;
            }

            if (!walker.Update(ped))
            {
                return;
            }

            currentStop = destination;
            if (travel == Travel.EnteringToDoor || travel == Travel.LeavingToDoor)
            {
                ped.Tasks.StandStill(1000);
                nextAction = now + GameClock.FromRealMilliseconds(1000);
                return;
            }
            if (travel == Travel.LeavingToOutside)
            {
                Clear();
                return;
            }
            if (travel == Travel.EnteringToReception)
            {
                travel = Travel.Inside;
                SetDepartureTime();
                nextAction = now;
                return;
            }

            if (currentStop == reservedAnchor)
            {
                RestAtAnchor(ped);
            }
            else
            {
                ped.Tasks.StandStill(8000);
                nextAction = now + GameClock.FromRealMilliseconds(random.Next(8000, 16001));
            }
        }

        internal void Clear()
        {
            walker.Clear();
            ReleaseAnchor();
            Actor.Clear();
        }

        private void SetDepartureTime() =>
            departureEligibleAt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(random.Next(60000, 120001));

        private void FollowPortal(Point target, Travel nextTravel)
        {
            Point[] waypoints = path.FindRoute(currentStop, target);
            if (waypoints == null || waypoints.Length == 0)
            {
                throw new InvalidOperationException("Sem rota de " + currentStop.Name + " até " + target.Name + ".");
            }

            Ped ped = Actor.Actor;
            LeaveStop(ped);
            travel = nextTravel;
            FollowRoute(ped, waypoints);
        }

        private void FindNextDestination(Ped ped)
        {
            var available = anchors.Where(anchor => anchor != currentStop && !path.IsAnchorReserved(anchor))
                .OrderBy(_ => random.Next());
            foreach (Point anchor in available)
            {
                Point[] waypoints = path.FindRoute(currentStop, anchor);
                if (waypoints != null && waypoints.Length > 0 && path.TryReserveAnchor(anchor))
                {
                    LeaveStop(ped);
                    reservedAnchor = anchor;
                    FollowRoute(ped, waypoints);
                    return;
                }
            }

            // Liberar o posto evita que NPCs em anchors ocupados fiquem esperando uns pelos outros.
            Point exit = currentStop.IsAnchor ? path.FindExit(currentStop) : null;
            if (exit != null)
            {
                LeaveStop(ped);
                FollowRoute(ped, new[] { exit });
            }
            else
            {
                nextAction = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(random.Next(6000, 12001));
            }
        }

        private void FollowRoute(Ped ped, Point[] waypoints)
        {
            destination = waypoints[waypoints.Length - 1];
            walker.Start(ped, currentStop, waypoints);
        }

        private void RestAtAnchor(Ped ped)
        {
            var animations = currentStop.Animations;
            int animationIndex = random.Next(animations.Length);
            if (animations.Length > 1)
            {
                var selected = animations[animationIndex];
                if (selected.Dictionary == lastAnimation.Dictionary && selected.Clip == lastAnimation.Clip)
                {
                    animationIndex = (animationIndex + random.Next(1, animations.Length)) % animations.Length;
                }
            }
            var animation = animations[animationIndex];
            int duration = currentStop.SeatedAnchor.HasValue
                ? random.Next(45000, 105001) : random.Next(25000, 65001);
            long stay = Role == AmbientRole.Patient
                ? GameClock.Minutes(currentStop.SeatedAnchor.HasValue ? random.Next(90, 181) : random.Next(60, 121))
                : GameClock.FromRealMilliseconds(duration);
            try
            {
                HospitalReceptionist.LoadAnimation(animation.Dictionary);
                Vector3 position = currentStop.SeatedAnchor ?? currentStop.Position;
                ped.Tasks.Clear();
                if (currentStop.SeatedAnchor.HasValue)
                {
                    NativeFunction.Natives.SET_ENTITY_COLLISION(ped, false, true);
                    NativeFunction.Natives.SET_ENTITY_COORDS_NO_OFFSET(ped,
                        position.X, position.Y, position.Z, false, false, false);
                    ped.IsPositionFrozen = true;
                    seated = true;
                }

                NativeFunction.Natives.TASK_PLAY_ANIM_ADVANCED(ped, animation.Dictionary, animation.Clip,
                    position.X, position.Y, position.Z, 0f, 0f, currentStop.Heading, 8f, 4f,
                    animation.Loop ? -1 : animation.Duration, animation.Loop ? (int)AnimationFlags.Loop : 0,
                    0f, 0, 0);
                if (animations.Length > 1)
                {
                    lastAnimation = (animation.Dictionary, animation.Clip);
                }
                nextAction = GameClock.NowMilliseconds + stay;
            }
            catch (InvalidOperationException error)
            {
                Game.LogTrivial("[Lumen Santé] Falha na animação de ambiente em " + currentStop.Name + ": " + error.Message);
                LeaveStop(ped);
                nextAction = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(6000);
            }
        }

        private void LeaveStop(Ped ped)
        {
            if (seated && ped != null && ped.Exists())
            {
                ped.IsPositionFrozen = false;
                if (ped.IsAlive)
                {
                    NpcWalking.TeleportToWaypoint(ped, currentStop.Position);
                }
                NativeFunction.Natives.SET_ENTITY_COLLISION(ped, true, true);
            }
            seated = false;
            ReleaseAnchor();
        }

        private void ReleaseAnchor()
        {
            if (reservedAnchor != null)
            {
                path.ReleaseAnchor(reservedAnchor);
                reservedAnchor = null;
            }
        }
    }
}
