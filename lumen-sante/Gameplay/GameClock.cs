using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal static class GameClock
    {
        private const int SecondsPerDay = 24 * 60 * 60;
        private const int GameMillisecondsPerRealMillisecond = 30;
        private static bool initialized;
        private static int previousSecondOfDay;
        private static long elapsedGameMilliseconds;

        internal static long NowMilliseconds => elapsedGameMilliseconds;

        internal static void Initialize()
        {
            previousSecondOfDay = ReadSecondOfDay();
            elapsedGameMilliseconds = 0;
            initialized = true;
        }

        internal static void Update()
        {
            int currentSecondOfDay = ReadSecondOfDay();
            if (!initialized)
            {
                previousSecondOfDay = currentSecondOfDay;
                initialized = true;
                return;
            }

            int elapsedSeconds = currentSecondOfDay - previousSecondOfDay;
            previousSecondOfDay = currentSecondOfDay;
            if (elapsedSeconds < 0)
            {
                elapsedSeconds += SecondsPerDay;
                if (elapsedSeconds > 600)
                {
                    elapsedSeconds = 0;
                }
            }

            // Ignore backward clock jumps while preserving normal midnight rollover.
            if (!Game.IsPaused)
            {
                elapsedGameMilliseconds += elapsedSeconds * 1000L;
            }
        }

        internal static long FromRealMilliseconds(int milliseconds) =>
            milliseconds * (long)GameMillisecondsPerRealMillisecond;

        internal static long Minutes(int minutes) => minutes * 60_000L;

        private static int ReadSecondOfDay() =>
            NativeFunction.Natives.GET_CLOCK_HOURS<int>() * 3600 +
            NativeFunction.Natives.GET_CLOCK_MINUTES<int>() * 60 +
            NativeFunction.Natives.GET_CLOCK_SECONDS<int>();
    }
}
