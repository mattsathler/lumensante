using System;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal sealed class ExamClipboard
    {
        private const string Dictionary = "amb@world_human_clipboard@male@base";
        private const string Clip = "base";
        private Ped owner;
        private Rage.Object clipboard;
        private bool failed;

        internal void Update(Ped doctor, bool visible)
        {
            if (!visible || doctor == null || !doctor.Exists() || !doctor.IsAlive || !doctor.IsOnFoot)
            {
                Clear();
                failed = false;
                return;
            }
            if (owner != null && owner != doctor) Clear();
            if (failed) return;

            try
            {
                if (owner == null)
                {
                    HospitalReceptionist.LoadAnimation(Dictionary);
                    if (!doctor.Exists() || !doctor.IsAlive || doctor != Game.LocalPlayer.Character) return;
                    owner = doctor;
                    clipboard = new Rage.Object(new Model("p_amb_clipboard_01"), doctor.Position);
                    if (!clipboard.Exists()) throw new InvalidOperationException("Prancheta indisponível.");
                    clipboard.IsPersistent = true;
                    int bone = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(doctor, 18905);
                    NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(clipboard, doctor, bone,
                        0.10f, 0.02f, 0.05f, 190f, 0f, 0f, false, false, false, true, 2, true);
                }

                if (!NativeFunction.Natives.IS_ENTITY_PLAYING_ANIM<bool>(doctor, Dictionary, Clip, 3))
                {
                    // Loop on the upper body as a secondary task, preserving locomotion.
                    doctor.Tasks.PlayAnimation(Dictionary, Clip, 4f, (AnimationFlags)49);
                }
            }
            catch (InvalidOperationException error)
            {
                Clear();
                failed = true;
                Game.LogTrivial("[Lumen Santé] Falha ao exibir prancheta: " + error.Message);
            }
        }

        internal void Clear()
        {
            try
            {
                if (owner != null && owner.Exists())
                {
                    NativeFunction.Natives.STOP_ANIM_TASK(owner, Dictionary, Clip, 4f);
                }
            }
            finally
            {
                owner = null;
                if (clipboard != null && clipboard.Exists()) clipboard.Delete();
                clipboard = null;
            }
        }
    }
}
