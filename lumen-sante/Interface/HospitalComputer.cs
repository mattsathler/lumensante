using System.Diagnostics;
using System;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using lumen_sante.Clinical;

namespace lumen_sante.Interface
{
    internal sealed class HospitalComputer
    {
        internal const string ComputerAnimation = "amb@prop_human_seat_computer@male@base";
        internal const string ComputerClip = "base";

        private readonly BedManagementPanel panel;
        private readonly UIMenu chairMenu;
        private readonly UIMenuItem interactWithPatient;
        private readonly UIMenuItem callPatient;
        private readonly PatientMenu patientMenu;
        private readonly Func<bool> canInteractWithPatient;
        private readonly Func<bool> canCallFromChair;
        private bool usingComputer;
        private bool tabletOpen;
        private bool usingPatientMenu;
        private Ped seatedDoctor;
        private Vector3 seatPosition;
        private Vector3 standingPosition;
        private float standingHeading;
        private bool hadCollision;

        internal HospitalComputer(MenuPool menuPool, string roomName, PatientMenu patientMenu,
            Func<bool> canInteractWithPatient, Func<bool> roomOccupied, Func<string> roomPatientName, Func<bool> canCallPatient,
            Func<bool> callNextPatient, Func<ExamOrders, bool> canCallSelectedPatient, Func<ExamOrders, bool> callSelectedPatient)
        {
            this.patientMenu = patientMenu;
            this.canInteractWithPatient = canInteractWithPatient;
            panel = new BedManagementPanel(roomOccupied, roomPatientName, canCallSelectedPatient);
            panel.PatientCallRequested += orders =>
            {
                if (IsSeated(Game.LocalPlayer.Character) && canCallFromChair() && canCallSelectedPatient(orders))
                    callSelectedPatient(orders);
            };
            canCallFromChair = () => !roomOccupied() && canCallPatient();
            chairMenu = new UIMenu("Cadeira", roomName);
            var useComputer = new UIMenuItem("Usar computador");
            callPatient = new UIMenuItem("Chamar próximo paciente") { Enabled = false };
            interactWithPatient = new UIMenuItem("Interagir com o paciente") { Enabled = false };
            var standUp = new UIMenuItem("Levantar da cadeira");
            chairMenu.AddItem(useComputer);
            chairMenu.AddItem(callPatient);
            chairMenu.AddItem(interactWithPatient);
            chairMenu.AddItem(standUp);
            menuPool.Add(chairMenu);

            useComputer.Activated += (sender, item) =>
            {
                chairMenu.Visible = false;
                usingComputer = true;
                panel.Open(chairMenu.SubtitleText);
            };
            callPatient.Activated += (sender, item) =>
            {
                if (IsSeated(Game.LocalPlayer.Character) && canCallFromChair())
                {
                    callNextPatient();
                    callPatient.Enabled = canCallFromChair();
                }
            };
            interactWithPatient.Activated += (sender, item) =>
            {
                if (IsOpen && canInteractWithPatient())
                {
                    chairMenu.Visible = false;
                    usingPatientMenu = true;
                    patientMenu.Open();
                }
            };
            standUp.Activated += (sender, item) => Close();
        }

        internal bool IsOpen => seatedDoctor != null;
        internal bool IsTabletOpen => tabletOpen;

        internal void OpenTablet()
        {
            if (IsOpen || tabletOpen) return;
            tabletOpen = true;
            usingComputer = true;
            panel.Open("Tablet", false);
        }
        internal event Action<ExamOrders, MedicationClass> MedicationToggled
        {
            add { panel.MedicationToggled += value; }
            remove { panel.MedicationToggled -= value; }
        }
        internal event Action<ExamOrders, ExamOrder> ExamRequested
        {
            add { panel.ExamRequested += value; }
            remove { panel.ExamRequested -= value; }
        }
        internal event Action<ExamOrders, string> TreatmentSelected
        {
            add { panel.TreatmentSelected += value; }
            remove { panel.TreatmentSelected -= value; }
        }

        internal bool IsSeated(Ped doctor) => IsOpen && doctor == seatedDoctor &&
            (doctor.Position - seatPosition).LengthSquared() <= 4f;

        internal void Sit(Ped doctor, string room, Vector3 interactionPoint, Vector3 position, float heading)
        {
            NativeFunction.Natives.REQUEST_ANIM_DICT(ComputerAnimation);
            var loadTimeout = Stopwatch.StartNew();
            while (!NativeFunction.Natives.HAS_ANIM_DICT_LOADED<bool>(ComputerAnimation))
            {
                if (loadTimeout.ElapsedMilliseconds >= 2000)
                {
                    Game.DisplayNotification("Não foi possível carregar a animação do computador.");
                    return;
                }

                GameFiber.Yield();
            }

            if (!doctor.Exists() || !doctor.IsAlive || !doctor.IsOnFoot ||
                doctor != Game.LocalPlayer.Character || (doctor.Position - interactionPoint).LengthSquared() > 2.25f)
            {
                return;
            }

            seatedDoctor = doctor;
            seatPosition = position;
            chairMenu.SubtitleText = room;
            standingPosition = doctor.Position;
            standingHeading = doctor.Heading;
            hadCollision = !NativeFunction.CallByHash<bool>(0xCCF1E97BEFDAE480UL, doctor);
            NativeFunction.Natives.SET_ENTITY_COLLISION(doctor, false, true);
            NativeFunction.Natives.SET_ENTITY_COORDS_NO_OFFSET(doctor,
                position.X, position.Y, position.Z, false, false, false);
            doctor.IsPositionFrozen = true;
            NativeFunction.Natives.TASK_PLAY_ANIM_ADVANCED(doctor, ComputerAnimation, ComputerClip,
                position.X, position.Y, position.Z, 0f, 0f, heading,
                8f, 4f, -1, (int)AnimationFlags.Loop, 0f, 0, 0);
            chairMenu.RefreshIndex();
            interactWithPatient.Enabled = canInteractWithPatient();
            callPatient.Enabled = canCallFromChair();
            chairMenu.Visible = true;
        }

        internal void SetPatients(BedManagementPanel.PatientEntry[] patients) => panel.SetPatients(patients);

        internal void RefreshExams(ExamOrders orders) => panel.RefreshExams(orders);

        internal void Update(Ped doctor, bool playerAvailable)
        {
            if (tabletOpen)
            {
                if (!playerAvailable || !panel.Update()) Close();
                return;
            }
            if (!IsOpen)
            {
                return;
            }

            interactWithPatient.Enabled = canInteractWithPatient();
            callPatient.Enabled = canCallFromChair();
            if (!playerAvailable || !IsSeated(doctor))
            {
                Close();
            }
            else if (usingComputer)
            {
                if (!panel.Update())
                {
                    panel.Close();
                    usingComputer = false;
                    chairMenu.Visible = true;
                }
            }
            else if (usingPatientMenu)
            {
                if (!interactWithPatient.Enabled || !patientMenu.IsOpen)
                {
                    patientMenu.Close();
                    usingPatientMenu = false;
                    chairMenu.Visible = true;
                }
            }
            else if (!chairMenu.Visible)
            {
                Close();
            }
        }

        internal void Close()
        {
            chairMenu.Visible = false;
            usingComputer = false;
            tabletOpen = false;
            usingPatientMenu = false;
            patientMenu.Close();
            panel.Close();
            if (seatedDoctor != null && seatedDoctor.Exists())
            {
                if (seatedDoctor.IsAlive)
                {
                    NativeFunction.Natives.STOP_ANIM_TASK(seatedDoctor, ComputerAnimation, ComputerClip, 4f);
                    seatedDoctor.Position = standingPosition;
                    seatedDoctor.Heading = standingHeading;
                }

                if (hadCollision)
                {
                    NativeFunction.Natives.SET_ENTITY_COLLISION(seatedDoctor, true, true);
                }

                seatedDoctor.IsPositionFrozen = false;
            }

            seatedDoctor = null;
        }
    }
}
