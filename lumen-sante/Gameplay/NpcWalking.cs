using System;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal static class NpcWalking
    {
        internal const float Speed = 1f;
        private const int TimeAllowanceMilliseconds = 3000;

        internal static int TimeoutFor(Vector3 origin, Vector3 destination)
        {
            double distance = Math.Sqrt((destination - origin).LengthSquared());
            return (int)Math.Ceiling(distance * 1000 / Speed) + TimeAllowanceMilliseconds;
        }

        internal static void GoStraight(Ped ped, Vector3 destination, float heading, int timeout)
        {
            NativeFunction.Natives.TASK_GO_STRAIGHT_TO_COORD(ped,
                destination.X, destination.Y, destination.Z, Speed, timeout, heading, 0.25f);
        }

        internal static void TeleportToWaypoint(Ped ped, Vector3 destination)
        {
            ped.Tasks.Clear();
            NativeFunction.Natives.SET_ENTITY_COORDS_NO_OFFSET(ped,
                destination.X, destination.Y, destination.Z, false, false, false);
        }
    }
}
