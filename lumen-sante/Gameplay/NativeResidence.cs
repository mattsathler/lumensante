using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    // Candidate positions from InteriorsV: uncalibrated in the target Legacy build.
    internal sealed class NativeResidence : IDisposable
    {
        internal sealed class Home
        {
            internal string Id, Name;
            internal Vector3 Outside, Inside, Probe;
            internal float OutsideHeading;
        }

        private static readonly Vector3 LowInside = new Vector3(265.1292f, -1000.950f, -100.0086f);
        private static readonly Vector3 LowProbe = new Vector3(261.4586f, -998.8196f, -99.00863f);
        private static readonly Vector3 MidInside = new Vector3(346.5050f, -1002.471f, -100.2062f);
        private static readonly Vector3 MidProbe = new Vector3(347.2686f, -999.2955f, -99.19622f);
        internal static readonly Home[] Homes =
        {
            new Home { Id = "sandy-zancudo-140", Name = "140 Zancudo Avenue · Sandy Shores", Outside = new Vector3(1900.1719f, 3772.9207f, 31.8744f), OutsideHeading = 210f, Inside = LowInside, Probe = LowProbe },
            new Home { Id = "grapeseed-1893", Name = "1893 Grapeseed Avenue · Grapeseed", Outside = new Vector3(1662.7830f, 4776.2407f, 41.0076f), OutsideHeading = 280f, Inside = LowInside, Probe = LowProbe },
            new Home { Id = "paleto-procopio-4401", Name = "4401 Procopio Drive · Paleto Bay", Outside = new Vector3(-302.4685f, 6327.2275f, 31.8920f), OutsideHeading = 40f, Inside = MidInside, Probe = MidProbe },
            new Home { Id = "los-santos-bay-city-0115", Name = "0115 Bay City Avenue · Los Santos", Outside = new Vector3(-969.9915f, -1431.375f, 6.6792f), OutsideHeading = 100f, Inside = MidInside, Probe = MidProbe }
        };
        private const float InteractionDistanceSquared = 2.0f * 2.0f;
        private int pinnedInterior;
        private Home occupiedHome;
        private Guid ownerId;
        private Ped occupant;
        private Blip residenceBlip;
        private string blipHomeId;
        private bool blipInside;
        private Ped routinePed;
        private readonly Stopwatch routineClock = new Stopwatch();
        private readonly Random routineRandom = new Random();
        internal bool Inside => occupiedHome != null;

        private static Home Find(string id) => Homes.FirstOrDefault(home => home.Id == id) ??
            (string.IsNullOrEmpty(id) || id == "executive-apartment-3" ? Homes[0] : null);

        internal void Update(Ped player, Guid doctorId, string residenceId, bool pressed, bool available)
        {
            if (routinePed != null)
            {
                bool cancel = player == null || !player.Exists() || !player.IsAlive || player != routinePed || !available ||
                    !Inside || routineClock.ElapsedMilliseconds >= 20000 || pressed ||
                    Game.IsKeyDown(Keys.W) || Game.IsKeyDown(Keys.A) || Game.IsKeyDown(Keys.S) || Game.IsKeyDown(Keys.D) ||
                    Game.IsKeyDown(Keys.Space) || Math.Abs(NativeFunction.Natives.GET_CONTROL_NORMAL<float>(0, 30)) > 0.2f ||
                    Math.Abs(NativeFunction.Natives.GET_CONTROL_NORMAL<float>(0, 31)) > 0.2f;
                if (cancel) StopRoutine();
            }
            if (Inside && (player == null || !player.Exists() || !player.IsAlive || player != occupant || doctorId != ownerId))
            {
                occupiedHome = null;
                occupant = null;
                ownerId = Guid.Empty;
                ReleaseInterior();
            }
            if (player == null || !player.Exists() || !player.IsAlive || doctorId == Guid.Empty)
            {
                SyncBlip(null, false);
                return;
            }
            Home home = Inside ? occupiedHome : Find(residenceId);
            if (home == null) { SyncBlip(null, false); return; }
            if (!Inside && available && player.IsOnFoot)
            {
                int playerInterior = NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>(player);
                int homeInterior = NativeFunction.Natives.GET_INTERIOR_AT_COORDS<int>(home.Probe.X, home.Probe.Y, home.Probe.Z);
                if (playerInterior != 0 && playerInterior == homeInterior)
                {
                    NativeFunction.Natives.PIN_INTERIOR_IN_MEMORY(homeInterior);
                    pinnedInterior = homeInterior;
                    occupiedHome = home;
                    ownerId = doctorId;
                    occupant = player;
                }
            }
            SyncBlip(home, Inside);
            if (!available || !player.IsOnFoot) return;
            Vector3 point = Inside ? home.Inside : home.Outside;
            if ((player.Position - point).LengthSquared() > InteractionDistanceSquared) return;
            Game.DisplayHelp(Inside ? "Pressione E para sair da residência." : "Pressione E para entrar na residência.", 1);
            if (pressed) Toggle(player, doctorId, residenceId);
        }

        internal void Toggle(Ped player, Guid doctorId, string residenceId, bool keepScreenFaded = false)
        {
            StopRoutine();
            if (player == null || !player.Exists() || !player.IsAlive || !player.IsOnFoot || doctorId == Guid.Empty)
            {
                Game.DisplayNotification("Acesse a residência a pé, com o médico disponível.");
                return;
            }
            if (Inside)
            {
                if (doctorId != ownerId || player != occupant) return;
                if (Move(player, occupiedHome.Outside, occupiedHome.OutsideHeading, keepScreenFaded))
                {
                    occupiedHome = null;
                    occupant = null;
                    ownerId = Guid.Empty;
                    ReleaseInterior();
                }
                return;
            }
            Home home = Find(residenceId);
            if (home == null)
            {
                Game.DisplayNotification("Selecione uma residência no menu do médico.");
                return;
            }
            NativeFunction.Natives.SET_FOCUS_POS_AND_VEL(home.Probe.X, home.Probe.Y, home.Probe.Z, 0f, 0f, 0f);
            bool sceneStarted = false;
            try
            {
            // Pinning an interior alone does not guarantee that the remote scene is streamed.
            // Only stop a scene load that this transition successfully started.
            sceneStarted = NativeFunction.Natives.NEW_LOAD_SCENE_START_SPHERE<bool>(
                home.Inside.X, home.Inside.Y, home.Inside.Z, 50f, 0);
            NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(home.Inside.X, home.Inside.Y, home.Inside.Z);
            var timeout = Stopwatch.StartNew();
            int interior = 0;
            while (timeout.ElapsedMilliseconds < 10000 && player.Exists() && player.IsAlive && player.IsOnFoot)
            {
                interior = NativeFunction.Natives.GET_INTERIOR_AT_COORDS<int>(home.Probe.X, home.Probe.Y, home.Probe.Z);
                if (interior != 0) break;
                GameFiber.Yield();
            }
            if (interior == 0 || !player.Exists() || !player.IsAlive || !player.IsOnFoot)
            {
                Game.LogTrivial("[Lumen Santé] Residência " + home.Id + ": interior não localizado após " + timeout.ElapsedMilliseconds + " ms.");
                Game.DisplayNotification("Não foi possível localizar a residência. Sua posição foi preservada.");
                return;
            }
            NativeFunction.Natives.PIN_INTERIOR_IN_MEMORY(interior);
            pinnedInterior = interior;
            bool wasDisabled = NativeFunction.Natives.IS_INTERIOR_DISABLED<bool>(interior);
            bool wasCapped = NativeFunction.Natives.IS_INTERIOR_CAPPED<bool>(interior);
            int reactivations = 0;
            void ActivateIfNeeded()
            {
                bool disabled = NativeFunction.Natives.IS_INTERIOR_DISABLED<bool>(interior);
                bool capped = NativeFunction.Natives.IS_INTERIOR_CAPPED<bool>(interior);
                if (!disabled && !capped) return;
                NativeFunction.Natives.SET_INTERIOR_ACTIVE(interior, true);
                if (disabled) NativeFunction.Natives.DISABLE_INTERIOR(interior, false);
                if (capped) NativeFunction.Natives.CAP_INTERIOR(interior, false);
                reactivations++;
            }
            ActivateIfNeeded();
            timeout.Restart();
            long nextActivationCheck = 250;
            while (timeout.ElapsedMilliseconds < 15000 && player.Exists() && player.IsAlive && player.IsOnFoot &&
                !NativeFunction.Natives.IS_INTERIOR_READY<bool>(interior))
            {
                if (timeout.ElapsedMilliseconds >= nextActivationCheck)
                {
                    ActivateIfNeeded();
                    nextActivationCheck = timeout.ElapsedMilliseconds + 250;
                }
                GameFiber.Yield();
            }
            if (!player.Exists() || !player.IsAlive || !player.IsOnFoot || !NativeFunction.Natives.IS_INTERIOR_READY<bool>(interior))
            {
                Game.LogTrivial("[Lumen Santé] Residência " + home.Id + ": interior " + interior + " não ficou pronto após " + timeout.ElapsedMilliseconds +
                    " ms; estado inicial desabilitado=" + wasDisabled + ", bloqueado=" + wasCapped +
                    "; estado final desabilitado=" + NativeFunction.Natives.IS_INTERIOR_DISABLED<bool>(interior) +
                    ", bloqueado=" + NativeFunction.Natives.IS_INTERIOR_CAPPED<bool>(interior) +
                    "; ativações=" + reactivations + ", cena iniciada=" + sceneStarted + ".");
                ReleaseInterior();
                Game.DisplayNotification("A residência não ficou pronta. Sua posição foi preservada.");
                return;
            }
            if (!Move(player, home.Inside, 0f, keepScreenFaded)) { ReleaseInterior(); return; }
            occupiedHome = home;
            occupant = player;
            ownerId = doctorId;
            }
            finally
            {
                if (sceneStarted) NativeFunction.Natives.NEW_LOAD_SCENE_STOP();
                NativeFunction.Natives.CLEAR_FOCUS();
            }
        }

        internal bool StartAtHome(Ped player, Guid doctorId, string residenceId, bool keepScreenFaded = false)
        {
            if (Inside) return occupant == player;
            Toggle(player, doctorId, residenceId, keepScreenFaded);
            if (!Inside || occupant != player || !player.Exists() || !player.IsAlive) return false;
            string scenario = routineRandom.Next(2) == 0 ? "WORLD_HUMAN_STAND_MOBILE" : "WORLD_HUMAN_DRINKING";
            NativeFunction.Natives.TASK_START_SCENARIO_IN_PLACE(player, scenario, 0, true);
            routinePed = player;
            routineClock.Restart();
            return true;
        }

        private void StopRoutine()
        {
            if (routinePed != null && routinePed.Exists() && routinePed.IsAlive &&
                Game.LocalPlayer.Character == routinePed)
                // Abort the scenario immediately so its game-owned prop is released too;
                // a gradual task clear can leave the phone or drink attached while walking.
                NativeFunction.Natives.CLEAR_PED_TASKS_IMMEDIATELY(routinePed);
            routinePed = null;
            routineClock.Reset();
        }

        private void SyncBlip(Home home, bool inside)
        {
            if (home == null)
            {
                if (residenceBlip != null && residenceBlip.Exists()) residenceBlip.Delete();
                residenceBlip = null;
                blipHomeId = null;
                return;
            }
            if (residenceBlip != null && residenceBlip.Exists() && blipHomeId == home.Id && blipInside == inside) return;
            if (residenceBlip != null && residenceBlip.Exists()) residenceBlip.Delete();
            residenceBlip = new Blip(inside ? home.Inside : home.Outside);
            residenceBlip.Name = inside ? "Sair da residência" : "Minha residência";
            NativeFunction.Natives.SET_BLIP_SPRITE(residenceBlip, 40);
            blipHomeId = home.Id;
            blipInside = inside;
        }

        private static bool Move(Ped player, Vector3 destination, float heading, bool keepScreenFaded = false)
        {
            Vector3 original = player.Position;
            float originalHeading = player.Heading;
            if (!keepScreenFaded) NativeFunction.Natives.DO_SCREEN_FADE_OUT(350);
            try
            {
                var timeout = Stopwatch.StartNew();
                while (timeout.ElapsedMilliseconds < 1500 && !NativeFunction.Natives.IS_SCREEN_FADED_OUT<bool>()) GameFiber.Yield();
                NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(destination.X, destination.Y, destination.Z);
                if (!player.Exists() || !player.IsAlive || !player.IsOnFoot) return false;
                player.Position = destination;
                player.Heading = heading;
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < 5000 && player.Exists() && player.IsAlive &&
                    !NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player)) GameFiber.Yield();
                if (!player.Exists() || !player.IsAlive) return false;
                if (!NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player))
                {
                    NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(original.X, original.Y, original.Z);
                    player.Position = original;
                    player.Heading = originalHeading;
                    Game.DisplayNotification("A colisão não ficou pronta. O personagem voltou à posição anterior.");
                    return false;
                }
                return true;
            }
            finally { if (!keepScreenFaded) NativeFunction.Natives.DO_SCREEN_FADE_IN(350); }
        }

        private void ReleaseInterior()
        {
            if (pinnedInterior == 0) return;
            NativeFunction.Natives.UNPIN_INTERIOR(pinnedInterior);
            pinnedInterior = 0;
        }

        public void Dispose()
        {
            StopRoutine();
            SyncBlip(null, false);
            // Keep the interior available if the plugin is unloaded while the doctor is inside.
            if (!Inside) ReleaseInterior();
        }
    }
}
