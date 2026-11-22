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
            if ((ped.Position - target.Position).LengthSquared() > reachDistanceSquared)
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
                Clear();
                return true;
            }

            WalkToNextPoint(ped);
            return false;
        }

        internal void Clear() => route = null;

        private void WalkToNextPoint(Ped ped)
        {
            Point target = route[nextPoint];
            float reachDistance = ReachDistanceFor(target);
            reachDistanceSquared = reachDistance * reachDistance;
            int timeout = NpcWalking.TimeoutFor(ped.Position, target.Position);
            NpcWalking.GoStraight(ped, target.Position, target.Heading, timeout);
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
                return 0.55f;
            }

            Point previous = nextPoint == 0 ? origin : route[nextPoint - 1];
            float segmentLength = (float)Math.Sqrt((target.Position - previous.Position).LengthSquared());
            return target.IsCurve
                ? Math.Min(0.55f, Math.Max(0.32f, segmentLength * 0.7f))
                : Math.Min(1.6f, Math.Max(0.55f, segmentLength * 0.55f));
        }
    }
}
