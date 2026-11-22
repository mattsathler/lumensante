using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace lumen_sante.Gameplay
{
    internal sealed class HospitalPath
    {
        internal sealed class Point
        {
            internal Point(string name, Vector3 position, float heading, bool isDoor = false,
                (string Dictionary, string Clip, int Duration, bool Loop)? animation = null, bool isCurve = false,
                Vector3? seatedAnchor = null,
                (string Dictionary, string Clip, int Duration, bool Loop)[] animationOptions = null, float? passingDistance = null)
            {
                Name = name;
                Position = position;
                Heading = heading;
                PassingDistance = passingDistance ?? (isDoor ? 1.3f : (float?)null);
                Animations = animationOptions ?? (animation.HasValue
                    ? new[] { animation.Value }
                    : Array.Empty<(string, string, int, bool)>());
                IsCurve = isCurve;
                SeatedAnchor = seatedAnchor;
            }

            internal string Name { get; }
            internal Vector3 Position { get; }
            internal float Heading { get; }
            internal float? PassingDistance { get; }
            internal (string Dictionary, string Clip, int Duration, bool Loop)[] Animations { get; }
            internal bool IsAnchor => Animations.Length > 0;
            internal bool IsCurve { get; }
            internal Vector3? SeatedAnchor { get; }

            internal static Point Seated(string name, Vector3 position, float heading,
                (string Dictionary, string Clip, int Duration, bool Loop)[] animations)
            {
                double radians = heading * Math.PI / 180;
                var approach = new Vector3(position.X - (float)Math.Sin(radians) * 0.55f,
                    position.Y + (float)Math.Cos(radians) * 0.55f, position.Z + 0.55f);
                return new Point(name, approach, heading, seatedAnchor: position, animationOptions: animations);
            }
        }

        private readonly Dictionary<Point, List<Point>> links = new Dictionary<Point, List<Point>>();
        private readonly HashSet<Point> occupiedAnchors = new HashSet<Point>();

        internal bool TryReserveAnchor(Point point) => occupiedAnchors.Add(point);

        internal bool IsAnchorReserved(Point point) => occupiedAnchors.Contains(point);

        internal void ReleaseAnchor(Point point) => occupiedAnchors.Remove(point);

        internal Point FindNearestWaypoint(Vector3 position, params Point[] excluded) => links.Keys
            .Where(point => !point.IsAnchor && !point.SeatedAnchor.HasValue && !excluded.Contains(point))
            .OrderBy(point => (point.Position - position).LengthSquared())
            .FirstOrDefault();

        internal Point FindExit(Point point) =>
            links.TryGetValue(point, out List<Point> destinations)
                ? destinations.Find(destination => !destination.IsAnchor)
                : null;

        internal static Point[] RoundCorners(Point origin, IReadOnlyList<Point> route)
        {
            var result = new List<Point>();
            for (int index = 0; index < route.Count; index++)
            {
                Point corner = route[index];
                Point previous = index == 0 ? origin : route[index - 1];
                if (index == route.Count - 1 || corner.PassingDistance.HasValue || corner.IsAnchor ||
                    !AddCornerCurve(result, previous, corner, route[index + 1]))
                {
                    result.Add(corner);
                }
            }
            return result.ToArray();
        }

        private static bool AddCornerCurve(List<Point> result, Point from, Point corner, Point to)
        {
            Vector3 incoming = corner.Position - from.Position;
            Vector3 outgoing = to.Position - corner.Position;
            float incomingLength = (float)Math.Sqrt(incoming.LengthSquared());
            float outgoingLength = (float)Math.Sqrt(outgoing.LengthSquared());
            float inset = Math.Min(0.9f, Math.Min(incomingLength, outgoingLength) * 0.22f);
            if (inset < 0.55f || Math.Abs(incoming.Z) > 0.25f || Math.Abs(outgoing.Z) > 0.25f)
            {
                return false;
            }

            float dot = (incoming.X * outgoing.X + incoming.Y * outgoing.Y + incoming.Z * outgoing.Z) /
                (incomingLength * outgoingLength);
            if (dot > 0.94f || dot < -0.8f)
            {
                return false;
            }

            Vector3 entry = corner.Position - incoming * (inset / incomingLength);
            Vector3 exit = corner.Position + outgoing * (inset / outgoingLength);
            for (int sample = 0; sample < 3; sample++)
            {
                float t = sample / 2f;
                float u = 1f - t;
                Vector3 position = entry * (u * u) + corner.Position * (2f * u * t) + exit * (t * t);
                Vector3 direction = (corner.Position - entry) * u + (exit - corner.Position) * t;
                float heading = (float)(Math.Atan2(-direction.X, direction.Y) * 180 / Math.PI + 360) % 360;
                result.Add(new Point(corner.Name, position, heading, isCurve: true));
            }
            return true;
        }

        internal void Connect(Point from, Point to)
        {
            if (!links.TryGetValue(from, out List<Point> destinations))
            {
                destinations = new List<Point>();
                links.Add(from, destinations);
            }

            destinations.Add(to);
        }

        internal void ConnectBothWays(Point first, Point second)
        {
            Connect(first, second);
            Connect(second, first);
        }

        internal Point[] FindRoute(Point origin, Point destination)
        {
            var distance = new Dictionary<Point, float> { [origin] = 0f };
            var previous = new Dictionary<Point, Point>();
            var pending = new HashSet<Point> { origin };
            while (pending.Count > 0)
            {
                Point point = pending.OrderBy(candidate => distance[candidate]).First();
                pending.Remove(point);
                if (point == destination)
                {
                    var route = new List<Point>();
                    while (point != origin)
                    {
                        route.Add(point);
                        point = previous[point];
                    }
                    route.Reverse();
                    return route.ToArray();
                }

                if (!links.TryGetValue(point, out List<Point> destinations))
                {
                    continue;
                }

                foreach (Point next in destinations)
                {
                    float cost = distance[point] + (float)Math.Sqrt((next.Position - point.Position).LengthSquared());
                    if (!distance.TryGetValue(next, out float existingCost) || cost < existingCost)
                    {
                        distance[next] = cost;
                        previous[next] = point;
                        pending.Add(next);
                    }
                }
            }

            return null;
        }
    }
}
