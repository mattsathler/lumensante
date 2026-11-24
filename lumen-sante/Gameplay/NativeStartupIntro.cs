using System;
using System.Diagnostics;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal static class NativeStartupIntro
    {
        // START_PLAYER_SWITCH: DESCENT_ONLY, SWITCH_TYPE_LONG.
        // The residence is already loaded and the startup fade is owned by the caller.
        internal static void Play(Ped player, Action reveal)
        {
            bool ownsSwitch = false;
            try
            {
                if (!CanContinue(player) || NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>()) return;

                NativeFunction.CallByHash<uint>(0xFAA23F2CBA159D67UL, player, player, 64, 1);
                ownsSwitch = true;
                var clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < 2000 && CanContinue(player) &&
                    !NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>())
                    GameFiber.Yield();

                if (!CanContinue(player) || !NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>())
                {
                    Game.LogTrivial("[Lumen Santé] Câmera de chegada indisponível; iniciando normalmente.");
                    return;
                }

                reveal();
                clock.Restart();
                while (clock.ElapsedMilliseconds < 20000 && CanContinue(player) &&
                    NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>())
                    GameFiber.Yield();
                if (clock.ElapsedMilliseconds >= 20000)
                    Game.LogTrivial("[Lumen Santé] Câmera de chegada interrompida após exceder o tempo limite.");
            }
            catch (Exception error)
            {
                Game.LogTrivial("[Lumen Santé] Falha na câmera de chegada: " + error.Message);
            }
            finally
            {
                try
                {
                    if (ownsSwitch && NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>())
                        NativeFunction.Natives.STOP_PLAYER_SWITCH();
                }
                catch (Exception error)
                {
                    Game.LogTrivial("[Lumen Santé] Falha ao encerrar câmera de chegada: " + error.Message);
                }
                finally { reveal(); }
            }
        }

        private static bool CanContinue(Ped player)
        {
            return !Game.IsLoading && !Game.IsPaused && player != null && player.Exists() && player.IsAlive &&
                Game.LocalPlayer.Character == player;
        }
    }
}
