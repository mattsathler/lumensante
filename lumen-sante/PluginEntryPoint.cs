using System;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using Rage.Native;
using System.Windows.Forms;
using Rage;
using Rage.Attributes;
using RAGENativeUI;
using RAGENativeUI.Elements;

[assembly: Plugin("Lumen Santé", Description = "Plantão no Central Los Santos Medical Center.")]

namespace lumen_sante
{
    public static class PluginEntryPoint
    {
        private static readonly Vector3 ShiftPoint = new Vector3(354.0f, -1397.0f, 32.0f);
        private static readonly Vector3 RoomInteractionPoint = new Vector3(357f, -1416f, 36f);
        private static readonly Vector3 PatientBedPosition = new Vector3(356.47f, -1417.32f, 36.48f);
        private const float BedHeading = 230f;
        private const int AdmissionDelayMilliseconds = 10000;
        private const int SeverityStageMilliseconds = 120000;
        private const string RoomName = "Room 1";
        private static readonly string[] MaleNames = { "Daniel", "Marcus", "Ethan", "Noah", "Lucas", "Gabriel" };
        private static readonly string[] FemaleNames = { "Maya", "Sofia", "Nina", "Olivia", "Elena", "Chloe" };
        private static readonly string[] LastNames = { "Brooks", "Carter", "Reyes", "Morgan", "Bennett", "Santos" };
        private static int shiftToggleRequests;

        private const float InteractionDistanceSquared = 36.0f;
        private const Keys InteractionKey = Keys.E;
        private const Keys PositionKey = Keys.F6;

        [ConsoleCommand(Name = "LumenSantePlantao", Description = "Inicia ou encerra o plantão do Lumen Santé.")]
        public static void Command_LumenSantePlantao()
        {
            Interlocked.Increment(ref shiftToggleRequests);
        }

        public static void Main()
        {
            Game.AddConsoleCommands();
            Game.LogTrivial("[Lumen Santé] Plugin carregado. Comando: LumenSantePlantao.");

            var menuPool = new MenuPool();
            var menu = new UIMenu("Lumen Santé", "Central Los Santos Medical Center");
            var shiftAction = new UIMenuItem("Iniciar plantão");
            menu.AddItem(shiftAction);
            menuPool.Add(menu);

            var patientMenu = new UIMenu("Paciente", RoomName);
            menuPool.Add(patientMenu);
            var vitalsMenu = menuPool.AddSubMenu(patientMenu, "Sinais vitais");
            var conversationMenu = menuPool.AddSubMenu(patientMenu, "Conversa com Paciente");
            var referralMenu = menuPool.AddSubMenu(patientMenu, "Encaminhar para exame");
            var labMenu = menuPool.AddSubMenu(patientMenu, "Solicitar exames");
            UIMenu[] patientMenus = { patientMenu, vitalsMenu, conversationMenu, referralMenu, labMenu };

            var pulse = new UIMenuItem("Frequência cardíaca") { Enabled = false };
            var pressure = new UIMenuItem("Pressão arterial") { Enabled = false };
            var breathing = new UIMenuItem("Frequência respiratória") { Enabled = false };
            var saturation = new UIMenuItem("Saturação de oxigênio") { Enabled = false };
            vitalsMenu.AddItem(pulse);
            vitalsMenu.AddItem(pressure);
            vitalsMenu.AddItem(breathing);
            vitalsMenu.AddItem(saturation);

            foreach (string exam in new[] { "Ressonância magnética (MRI)", "Radiografia (RX)", "Exames cardíacos" })
            {
                referralMenu.AddItem(new UIMenuItem(exam, "Disponível em uma etapa futura.") { Enabled = false });
            }

            foreach (string exam in new[] { "Painel hematológico", "Função renal", "Função hepática", "Eletrólitos" })
            {
                labMenu.AddItem(new UIMenuItem(exam, "Disponível em uma etapa futura.") { Enabled = false });
            }

            var admissionHud = new TimerBarPool();
            var admissionAlert = new TextTimerBar(RoomName, string.Empty);
            admissionHud.Add(admissionAlert);
            var random = new Random();
            string patientName = null;
            int severityIndex = 0;
            int terminalIndex = LungCancerCase.Stages.Length - 1;
            var severityClock = new Stopwatch();
            var admissionClock = new Stopwatch();
            var alertClock = new Stopwatch();
            Ped patient = null;
            Ped shiftPlayer = null;

            void AddQuestion(string question, Func<string> answer)
            {
                var item = new UIMenuItem(question);
                conversationMenu.AddItem(item);
                item.Activated += (sender, selectedItem) =>
                {
                    if (patient == null || !patient.Exists() || !patient.IsAlive || severityIndex == terminalIndex)
                    {
                        return;
                    }

                    Game.DisplaySubtitle(patientName + ": " + answer(), 7000);
                    Game.LogTrivial("[Lumen Santé] " + patientName + " respondeu: " + question);
                };
            }

            AddQuestion("Como está se sentindo?", () => LungCancerCase.Stages[severityIndex].Symptoms);
            AddQuestion("Os sintomas começaram há quanto tempo?", () =>
                "Comecei a tossir há algumas semanas. Achei que fosse passar.");
            AddQuestion("Sente falta de ar?", () => severityIndex < 2 ? "Só quando faço esforço."
                : severityIndex < 6 ? "Sim, até em caminhadas curtas." : "Sim, mesmo sem sair da cama.");
            AddQuestion("Notou sangue ao tossir?", () => severityIndex < 5 ? "Até agora, não."
                : "Sim, vi sangue misturado ao catarro.");
            AddQuestion("Perdeu peso recentemente?", () => "Minhas roupas ficaram mais folgadas no último mês.");

            void ClosePatientMenus()
            {
                foreach (UIMenu patientView in patientMenus)
                {
                    patientView.Visible = false;
                }
            }

            void ShowVitals()
            {
                LungCancerCase stage = LungCancerCase.Stages[severityIndex];
                pulse.RightLabel = stage.Pulse + " bpm";
                pressure.RightLabel = stage.Systolic + "/" + stage.Diastolic + " mmHg";
                breathing.RightLabel = stage.RespiratoryRate + " irpm";
                saturation.RightLabel = stage.OxygenSaturation + "%";
            }

            void ClearAdmission()
            {
                admissionClock.Reset();
                alertClock.Reset();
                severityClock.Reset();
                severityIndex = 0;
                ClosePatientMenus();
                if (patient != null && patient.Exists())
                {
                    patient.Delete();
                }

                patient = null;
            }
            void ToggleShift(Ped player)
            {
                bool starting = shiftPlayer == null;
                if (starting && (!player.Exists() || !player.IsAlive))
                {
                    return;
                }

                shiftPlayer = starting ? player : null;
                if (starting)
                {
                    admissionClock.Restart();
                }
                else
                {
                    ClearAdmission();
                }

                shiftAction.Text = starting ? "Encerrar plantão" : "Iniciar plantão";
                string message = starting ? "Plantão iniciado." : "Plantão encerrado.";
                Game.DisplayNotification(message);
                Game.LogTrivial("[Lumen Santé] " + message);
                menu.Visible = false;
            }

            shiftAction.Activated += (sender, item) => ToggleShift(Game.LocalPlayer.Character);

            var shiftBlip = new Blip(ShiftPoint);
            try
            {
                shiftBlip.Name = "Lumen Santé - Plantão";
                shiftBlip.Sprite = BlipSprite.Hospital;

                while (true)
                {
                    GameFiber.Yield();

                    Ped player = Game.LocalPlayer.Character;
                    bool playerAvailable = player.Exists() && player.IsAlive;
                    if (playerAvailable && Game.IsKeyDown(PositionKey))
                    {
                        Vector3 position = player.Position;
                        string coordinates = string.Format(CultureInfo.InvariantCulture,
                            "X: {0:F2}  Y: {1:F2}  Z: {2:F2}  H: {3:F1}°", position.X, position.Y, position.Z, player.Heading);
                        Game.DisplayNotification(coordinates);
                        Game.LogTrivial("[Lumen Santé] " + coordinates);
                    }

                    if (shiftPlayer != null && (!playerAvailable || player != shiftPlayer))
                    {
                        shiftPlayer = null;
                        ClearAdmission();
                        shiftAction.Text = "Iniciar plantão";
                        menu.Visible = false;
                        Game.LogTrivial("[Lumen Santé] Plantão interrompido.");
                    }

                    if ((Interlocked.Exchange(ref shiftToggleRequests, 0) & 1) != 0)
                    {
                        ToggleShift(player);
                    }

                    if (admissionClock.IsRunning && admissionClock.ElapsedMilliseconds >= AdmissionDelayMilliseconds)
                    {
                        admissionClock.Reset();
                        try
                        {
                            bool female = random.Next(2) == 0;
                            string model = female ? "u_f_m_corpse_01" : "u_m_y_corpse_01";
                            string[] names = female ? FemaleNames : MaleNames;
                            patientName = names[random.Next(names.Length)] + " " +
                                LastNames[random.Next(LastNames.Length)];
                            patient = new Ped(model, PatientBedPosition, BedHeading);
                            if (!patient.Exists())
                            {
                                throw new System.InvalidOperationException("NPC indisponível.");
                            }

                            patient.BlockPermanentEvents = true;
                            patient.IsPositionFrozen = true;
                            patient.Tasks.PlayAnimation("amb@world_human_sunbathe@male@back@base", "base", 1f, AnimationFlags.Loop);
                            severityIndex = 0;
                            ShowVitals();
                            severityClock.Restart();
                            admissionAlert.Text = patientName;
                            alertClock.Restart();
                            Game.LogTrivial("[Lumen Santé] Internado: " + patientName + ", " + RoomName + ".");
                        }
                        catch (System.Exception error)
                        {
                            ClearAdmission();
                            Game.DisplayNotification("A admissão no Room 1 falhou.");
                            Game.LogTrivial("[Lumen Santé] Falha na admissão: " + error.Message);
                        }
                    }

                    if (patient != null && (!patient.Exists() || (!patient.IsAlive && severityIndex != terminalIndex)))
                    {
                        ClearAdmission();
                        Game.LogTrivial("[Lumen Santé] Paciente indisponível. Leito liberado.");
                    }

                    if (severityClock.IsRunning)
                    {
                        int currentIndex = (int)Math.Min(terminalIndex,
                            severityClock.ElapsedMilliseconds / SeverityStageMilliseconds);
                        if (currentIndex != severityIndex)
                        {
                            severityIndex = currentIndex;
                            ShowVitals();

                            if (severityIndex == terminalIndex)
                            {
                                severityClock.Stop();
                                ClosePatientMenus();
                                patient.IsPositionFrozen = false;
                                patient.Health = 0;
                                Game.DisplayNotification(patientName + " veio a óbito no " + RoomName + ".");
                                Game.LogTrivial("[Lumen Santé] Óbito: " + patientName + ", " + RoomName + ".");
                            }
                        }
                    }

                    if (alertClock.IsRunning && alertClock.ElapsedMilliseconds < 8000)
                    {
                        admissionHud.Draw();
                    }

                    bool onFoot = playerAvailable && player.IsOnFoot;
                    bool atReception = onFoot &&
                        (player.Position - ShiftPoint).LengthSquared() <= InteractionDistanceSquared;
                    bool atBed = onFoot && patient != null && severityIndex != terminalIndex &&
                        (player.Position - RoomInteractionPoint).LengthSquared() <= 2.25f;

                    if (!atReception)
                    {
                        menu.Visible = false;
                    }

                    if (!atBed)
                    {
                        ClosePatientMenus();
                    }

                    if (!menu.Visible && !Array.Exists(patientMenus, view => view.Visible) && (atReception || atBed))
                    {
                        Game.DisplayHelp(atBed ? "Pressione E para interagir com o paciente."
                            : "Pressione E para abrir o menu de plantão.", 1);
                        if (Game.IsKeyDown(InteractionKey))
                        {
                            (atBed ? patientMenu : menu).Visible = true;
                        }
                    }

                    menuPool.ProcessMenus();
                }
            }
            finally
            {
                menu.Visible = false;
                ClearAdmission();
                if (shiftBlip.Exists())
                {
                    shiftBlip.Delete();
                }
            }
        }
    }
}
