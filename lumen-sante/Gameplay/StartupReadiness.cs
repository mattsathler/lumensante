namespace lumen_sante.Gameplay
{
    // Require the same ready player and a stable position continuously before applying a profile.
    internal sealed class StartupReadiness
    {
        private const long StableMilliseconds = 4000;
        private long stableSince = -1;
        private int stablePlayer;
        private float x, y, z;

        internal bool Observe(bool ready, int player, float px, float py, float pz, long now)
        {
            if (!ready || player == 0)
            {
                stableSince = -1;
                return false;
            }
            float dx = px - x, dy = py - y, dz = pz - z;
            if (stableSince < 0 || player != stablePlayer || now < stableSince || dx * dx + dy * dy + dz * dz > 4f)
            {
                stableSince = now;
                stablePlayer = player;
                x = px; y = py; z = pz;
                return false;
            }
            return now - stableSince >= StableMilliseconds;
        }
    }
}
