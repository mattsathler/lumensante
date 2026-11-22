using System;
using System.Collections.Generic;
using Rage;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class NpcRoute
    {
        private Point origin;
        private Point[] route;
        private int nextPoint;
        private long deadline;
        private float reachDistanceSquared;
        private bool teleportOnTimeout;
        private bool virtualWalking;
        internal static bool BackgroundSimulation { get; set; }

        internal bool IsWalking => route != null;
        internal Point LastGraphPoint { get; private set; }

        internal void Start(Ped ped, Point from, IReadOnlyList<Point> waypoints, bool teleportOnTimeout = true)
        {
            origin = from;
            LastGraphPoint = from;
            this.teleportOnTimeout = teleportOnTimeout;
            route = HospitalPath.RoundCorners(origin, waypoints);
            nextPoint = 0;
            WalkToNextPoint(ped);
        }

        internal bool Update(Ped ped)
        {
            Point target = route[nextPoint];
            if (virtualWalking != BackgroundSimulation)
            {
                ped.Tasks.Clear();
                if (!BackgroundSimulation) NpcWalking.TeleportToWaypoint(ped, (LastGraphPoint ?? origin).Position);
                WalkToNextPoint(ped);
                return false;
            }
            if (virtualWalking)
            {
                if (GameClock.NowMilliseconds < deadline) return false;
            }
            else if ((ped.Position - target.Position).LengthSquared() > reachDistanceSquared)
            {
                if (GameClock.NowMilliseconds < deadline)
                {
                    return false;
                }

                if (teleportOnTimeout)
                {
                    NpcWalking.TeleportToWaypoint(ped, target.Position);
                    Game.LogTrivial("[Lumen Santé] NPC reposicionado em " + target.Name + ".");
                }
                else
                {
                    ped.Tasks.Clear();
                    WalkToNextPoint(ped);
                    Game.LogTrivial("[Lumen Santé] Nova tentativa de caminhar até " + target.Name + ".");
                    return false;
                }
            }

            if (!target.IsCurve)
            {
                LastGraphPoint = target;
            }

            if (++nextPoint == route.Length)
            {
                if (virtualWalking) NpcWalking.TeleportToWaypoint(ped, target.Position);
                Clear();
                return true;
            }

            WalkToNextPoint(ped);
            return false;
        }

        internal void Clear()
        {
            route = null;
            virtualWalking = false;
        }

        private void WalkToNextPoint(Ped ped)
        {
            Point target = route[nextPoint];
            float reachDistance = ReachDistanceFor(target);
            reachDistanceSquared = reachDistance * reachDistance;
            virtualWalking = BackgroundSimulation;
            Vector3 from = nextPoint == 0 ? origin.Position : route[nextPoint - 1].Position;
            int timeout = virtualWalking
                ? Math.Max(250, (int)Math.Ceiling(Math.Sqrt((target.Position - from).LengthSquared()) * 1000 / NpcWalking.Speed))
                : NpcWalking.TimeoutFor(ped.Position, target.Position);
            if (!virtualWalking) NpcWalking.GoStraight(ped, target.Position, target.Heading, timeout);
            deadline = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(timeout);
        }

        private float ReachDistanceFor(Point target)
        {
            if (target.PassingDistance.HasValue)
            {
                return target.PassingDistance.Value;
            }

            if (nextPoint == route.Length - 1)
            {
                return 1.5f;
            }

            Point previous = nextPoint == 0 ? origin : route[nextPoint - 1];
            float segmentLength = (float)Math.Sqrt((target.Position - previous.Position).LengthSquared());
            return target.IsCurve
                ? Math.Min(0.85f, Math.Max(0.55f, segmentLength * 0.7f))
                : Math.Min(2f, Math.Max(1f, segmentLength * 0.55f));
        }
    }
}
