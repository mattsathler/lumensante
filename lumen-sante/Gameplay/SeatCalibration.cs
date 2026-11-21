using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rage;
using lumen_sante.Interface;

namespace lumen_sante.Gameplay
{
    internal sealed class SeatCalibration
    {
        private static readonly (string Name, string Dictionary, string Clip)[] Animations = new (string Name, string Dictionary, string Clip)[]
        {
            ("Espera", HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip),
            ("Computador", HospitalComputer.ComputerAnimation, HospitalComputer.ComputerClip),
            WaitingRoom.ElbowOnKnee,
            WaitingRoom.FootForward
        }.Concat(BedAnimations.Poses).ToArray();

        private readonly IEnumerable<HospitalReceptionist> seatedActors;
        private readonly HospitalReceptionist preview =
            new HospitalReceptionist(Animations[0].Dictionary, Animations[0].Clip);
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private Ped operatorPed;
        private Vector3 position;
        private float heading;
        private long nextAdjustment;
        private int animationIndex;
        private bool animationKeyWasDown;
        private bool copyKeyWasDown;

        internal SeatCalibration(IEnumerable<HospitalReceptionist> seatedActors)
        {
            this.seatedActors = seatedActors;
        }

        internal bool IsActive => operatorPed != null;

        internal void Update(Ped player, bool playerAvailable, bool toggleRequested)
        {
            if (operatorPed != null && (!playerAvailable || operatorPed != player || !preview.Exists))
            {
                Close();
            }

            if (toggleRequested)
            {
                if (operatorPed != null)
                {
                    Close();
                }
                else if (playerAvailable && player.IsOnFoot)
                {
                    Open(player);
                }
                else
                {
                    Game.DisplayNotification("Fique a pé para calibrar o assento.");
                }

                return;
            }

            if (operatorPed == null)
            {
                return;
            }

            bool copyKeyDown = Game.IsKeyDown(Keys.NumPad5);
            bool copyPressed = copyKeyDown && !copyKeyWasDown;
            copyKeyWasDown = copyKeyDown;
            if (copyPressed)
            {
                string result = FormatPosition();
                string clipboardText = FormattableString.Invariant(
                    $"{position.X:F3} {position.Y:F3} {position.Z:F3} {heading:F1}");
                Game.LogTrivial("[Lumen Santé] Assento calibrado: " + result);
                bool copied = CopyCoordinates(clipboardText);
                Game.DisplaySubtitle(result + (copied ? "  |  Copiado" : "  |  Registrado no log"), 8000);
                return;
            }

            if (Game.IsKeyDown(Keys.NumPad0))
            {
                Close();
                Game.DisplaySubtitle("Calibração cancelada.", 3000);
                return;
            }

            bool nextAnimation = Game.IsKeyDown(Keys.Add);
            bool previousAnimation = Game.IsKeyDown(Keys.Subtract);
            bool animationKeyDown = nextAnimation || previousAnimation;
            bool switchAnimation = animationKeyDown && !animationKeyWasDown;
            animationKeyWasDown = animationKeyDown;
            if (switchAnimation)
            {
                ChangeAnimation(nextAnimation ? 1 : -1);
                return;
            }

            long now = clock.ElapsedMilliseconds;
            if (now < nextAdjustment)
            {
                return;
            }

            float step = Game.IsKeyDown(Keys.ShiftKey) ? 0.01f : 0.05f;
            var offset = new Vector3(
                Axis(Keys.NumPad6, Keys.NumPad4) * step,
                Axis(Keys.NumPad8, Keys.NumPad2) * step,
                Axis(Keys.NumPad9, Keys.NumPad3) * step);
            int rotation = Axis(Keys.NumPad7, Keys.NumPad1);
            if (offset.LengthSquared() == 0f && rotation == 0)
            {
                return;
            }

            nextAdjustment = now + 120;
            position += offset;
            heading = (heading + rotation + 360f) % 360f;
            preview.PlaceAt(position, heading);
            ShowPreview();
        }

        private void Open(Ped player)
        {
            position = player.Position;
            heading = player.Heading;
            try
            {
                preview.CreateAt(position, heading);
                operatorPed = player;
                SetSeatedActorsVisible(false);
                ShowPreview();
            }
            catch (Exception error)
            {
                Close();
                Game.LogTrivial("[Lumen Santé] Falha na calibração de assento: " + error.Message);
                Game.DisplayNotification("Não foi possível criar o NPC de calibração.");
            }
        }

        internal void Close()
        {
            preview.Clear();
            SetSeatedActorsVisible(true);
            operatorPed = null;
            nextAdjustment = 0;
            animationKeyWasDown = false;
            copyKeyWasDown = false;
        }

        private void ChangeAnimation(int direction)
        {
            int nextIndex = (animationIndex + direction + Animations.Length) % Animations.Length;
            var animation = Animations[nextIndex];
            try
            {
                preview.ChangeAnimation(animation.Dictionary, animation.Clip, position, heading);
                animationIndex = nextIndex;
                ShowPreview();
            }
            catch (Exception error)
            {
                Game.LogTrivial("[Lumen Santé] Falha ao trocar a animação de assento: " + error.Message);
                Game.DisplayNotification("Não foi possível carregar a animação " + animation.Name + ".");
            }
        }

        private void ShowPreview() =>
            Game.DisplaySubtitle(Animations[animationIndex].Name + "  |  " + FormatPosition(), 5000);

        private void SetSeatedActorsVisible(bool visible)
        {
            foreach (HospitalReceptionist actor in seatedActors)
            {
                actor.SetVisible(visible);
            }
        }

        private string FormatPosition() => FormattableString.Invariant(
            $"X={position.X:F3}  Y={position.Y:F3}  Z={position.Z:F3}  H={heading:F1}°");

        private static bool CopyCoordinates(string coordinates)
        {
            var completion = new TaskCompletionSource<Exception>();
            var clipboardThread = new Thread(() =>
            {
                try
                {
                    Clipboard.SetDataObject(coordinates, true, 5, 50);
                    completion.SetResult(null);
                }
                catch (Exception error)
                {
                    completion.SetResult(error);
                }
            }) { IsBackground = true };

            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.Start();
            while (!completion.Task.IsCompleted)
            {
                GameFiber.Yield();
            }

            Exception failure = completion.Task.Result;
            if (failure != null)
            {
                Game.LogTrivial("[Lumen Santé] Falha ao copiar as coordenadas: " + failure.Message);
            }

            return failure == null;
        }

        private static int Axis(Keys positive, Keys negative) =>
            (Game.IsKeyDown(positive) ? 1 : 0) - (Game.IsKeyDown(negative) ? 1 : 0);
    }
}
