using System;
using System.Diagnostics;
using Rage;
using Rage.Native;
using lumen_sante.Interface;

namespace lumen_sante.Gameplay
{
    internal sealed class HospitalReceptionist
    {
        private const string ReceptionistModel = "a_f_y_business_01";
        private readonly string modelName;
        private string animationDictionary;
        private string animationClip;
        private Ped receptionist;

        internal HospitalReceptionist(
            string animationDictionary = HospitalComputer.ComputerAnimation,
            string animationClip = HospitalComputer.ComputerClip,
            string modelName = ReceptionistModel, string employeeName = null)
        {
            this.animationDictionary = animationDictionary;
            this.animationClip = animationClip;
            this.modelName = modelName;
            Name = employeeName;
        }

        internal string Name { get; }

        internal void Create(Vector3 position, float heading)
        {
            try
            {
                CreateAt(position, heading);
                Game.LogTrivial(FormattableString.Invariant(
                    $"[Lumen Santé] Secretária criada em X={position.X:F3}, Y={position.Y:F3}, Z={position.Z:F3}, H={heading:F1}."));
            }
            catch (Exception error)
            {
                Clear();
                Game.LogTrivial("[Lumen Santé] Falha ao criar a secretária: " + error.Message);
            }
        }

        internal void CreateAt(Vector3 position, float heading)
        {
            LoadAnimation(animationDictionary);

            Seat(new Ped(new Model(modelName), position, heading), position, heading);
        }

        internal void CreateStanding(Vector3 position, float heading) =>
            Track(new Ped(new Model(modelName), position, heading));

        internal void CreateStanding(Ped ped) => Track(ped);

        private void Track(Ped ped)
        {
            receptionist = ped;
            if (!receptionist.Exists())
            {
                throw new InvalidOperationException("NPC indisponível.");
            }

            receptionist.IsPersistent = true;
            receptionist.BlockPermanentEvents = true;
        }

        internal void Seat(Ped ped, Vector3 position, float heading)
        {
            Track(ped);
            receptionist.Tasks.Clear();
            NativeFunction.Natives.SET_ENTITY_COLLISION(receptionist, false, true);
            PlaceAt(position, heading);
            receptionist.IsPositionFrozen = true;
        }

        internal bool Exists => receptionist != null && receptionist.Exists();
        internal bool IsAlive => Exists && receptionist.IsAlive;
        internal Ped Actor => receptionist;

        internal Ped Release()
        {
            Ped ped = receptionist;
            receptionist = null;
            return ped;
        }

        internal void ChangeAnimation(string dictionary, string clip, Vector3 position, float heading)
        {
            LoadAnimation(dictionary);
            animationDictionary = dictionary;
            animationClip = clip;
            PlaceAt(position, heading);
        }

        internal static void LoadAnimation(string dictionary)
        {
            NativeFunction.Natives.REQUEST_ANIM_DICT(dictionary);
            var loadTimeout = Stopwatch.StartNew();
            while (!NativeFunction.Natives.HAS_ANIM_DICT_LOADED<bool>(dictionary))
            {
                if (loadTimeout.ElapsedMilliseconds >= 2000)
                {
                    throw new InvalidOperationException("A animação do assento não carregou: " + dictionary);
                }

                GameFiber.Yield();
            }
        }

        internal void SetVisible(bool visible)
        {
            if (Exists)
            {
                NativeFunction.Natives.SET_ENTITY_VISIBLE(receptionist, visible, false);
            }
        }

        internal void PlaceAt(Vector3 position, float heading) =>
            PlaceAt(receptionist, position, heading, animationDictionary, animationClip);

        internal static void PlaceAt(Ped ped, Vector3 position, float heading, string dictionary, string clip)
        {
            NativeFunction.Natives.SET_ENTITY_COORDS_NO_OFFSET(ped,
                position.X, position.Y, position.Z, false, false, false);
            NativeFunction.Natives.TASK_PLAY_ANIM_ADVANCED(ped, dictionary, clip,
                position.X, position.Y, position.Z, 0f, 0f, heading,
                8f, 4f, -1, (int)AnimationFlags.Loop, 0f, 0, 0);
        }

        internal void Clear()
        {
            if (receptionist != null && receptionist.Exists())
            {
                receptionist.Delete();
            }

            receptionist = null;
        }
    }
}
