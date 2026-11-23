using System;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using lumen_sante.Clinical;

namespace lumen_sante.Interface
{
    internal sealed class PatientMenu
    {
        private readonly UIMenu root;
        private readonly UIMenu examination;
        private readonly UIMenu inpatient;
        private readonly UIMenu inspectionMenu;
        private readonly UIMenu vitalsMenu;
        private readonly UIMenu conversation;
        private readonly UIMenu inpatientConversation;
        private readonly UIMenu inpatientInspection;
        private readonly UIMenu inpatientVitals;
        private readonly Func<bool> canTalk;
        private ExamOrders currentOrders;
        private readonly UIMenu[] views;
        private readonly UIMenuItem[][] inspectionRows;
        private readonly UIMenuItem[][] vitalsRows;
        private string patientName;
        private int severityIndex;


        internal PatientMenu(MenuPool pool, string roomName, Func<bool> canTalk)
        {
            this.canTalk = canTalk;
            root = new UIMenu("Paciente", roomName);
            examination = new UIMenu("Paciente", "Maca de exames");
            inpatient = new UIMenu("Paciente", "Internação");
            pool.Add(root);
            pool.Add(examination);
            pool.Add(inpatient);
            inspectionMenu = MenuPalette.SubMenu(pool, examination, "Avaliação geral");
            vitalsMenu = MenuPalette.SubMenu(pool, examination, "Sinais vitais");
            inpatientInspection = MenuPalette.SubMenu(pool, inpatient, "Avaliação geral");
            inpatientVitals = MenuPalette.SubMenu(pool, inpatient, "Sinais vitais");
            conversation = MenuPalette.SubMenu(pool, root, "Conversa com Paciente");
            inpatientConversation = MenuPalette.SubMenu(pool, inpatient, "Conversa com Paciente");


            var lieDown = MenuPalette.Item("Pedir para se deitar");
            root.AddItem(lieDown);
            var admissionMenu = MenuPalette.SubMenu(pool, root, "Internar Paciente");
            var returnToChair = MenuPalette.Item("Retornar paciente à cadeira");
            var commonBed = MenuPalette.Item("Quarto");
            admissionMenu.AddItem(commonBed);
            commonBed.Activated += (sender, item) => AdmissionRequested?.Invoke(InpatientUnit.Room);
            var wardBed = MenuPalette.Item("Enfermaria");
            admissionMenu.AddItem(wardBed);
            wardBed.Activated += (sender, item) => AdmissionRequested?.Invoke(InpatientUnit.Ward);
            examination.AddItem(returnToChair);
            var emergency = MenuPalette.Item("Emergência", ActionTone.Destructive);
            emergency.Enabled = false;
            emergency.DisabledForeColor = MenuPalette.ActionText(ActionTone.Destructive);
            admissionMenu.AddItem(emergency);
            lieDown.Activated += (sender, item) => LieDownRequested?.Invoke();
            returnToChair.Activated += (sender, item) => ChairRequested?.Invoke();
            var dischargeMenus = new[] { root, inpatient }.Select(parent =>
            {
                UIMenu confirmation = MenuPalette.SubMenu(pool, parent,
                    parent == root ? "Alta Hospitalar" : "Alta hospitalar", ActionTone.Accent);
                confirmation.SubtitleText = "Confirmar a alta deste paciente?";
                var followUp = MenuPalette.Apply(new UIMenuCheckboxItem("Agendar retorno?", false));
                var confirm = MenuPalette.Item("Dar Alta", ActionTone.Accent);
                confirmation.AddItem(followUp);
                confirmation.AddItem(confirm);
                confirmation.OnMenuOpen += menu => followUp.Checked = false;
                confirm.Activated += (sender, item) =>
                {
                    if (!canTalk() || currentOrders == null) return;
                    ExamOrders discharged = currentOrders;
                    bool schedule = followUp.Checked;
                    Close();
                    DischargeRequested?.Invoke(discharged, schedule);
                };
                return confirmation;
            }).ToArray();
            views = new[] { root, examination, inpatient, inspectionMenu, vitalsMenu, inpatientInspection,
                inpatientVitals, conversation, inpatientConversation, admissionMenu }
                .Concat(dischargeMenus).ToArray();

            var inspectionLabels = new[] { "Estado geral", "Respiração", "Pele e lábios", "Temperatura corporal" };
            var vitalsLabels = new[] { "Frequência cardíaca", "Pressão arterial", "Frequência respiratória",
                "Saturação de oxigênio", "Temperatura corporal" };
            inspectionMenu.SubtitleText = inpatientInspection.SubtitleText = "Avaliação geral · Achados e temperatura";
            inspectionRows = new[] { AddReadOnlyRows(inspectionMenu, inspectionLabels), AddReadOnlyRows(inpatientInspection, inspectionLabels) };
            vitalsRows = new[] { AddReadOnlyRows(vitalsMenu, vitalsLabels), AddReadOnlyRows(inpatientVitals, vitalsLabels) };
        }

        private static UIMenuItem[] AddReadOnlyRows(UIMenu menu, string[] labels)
        {
            return Array.ConvertAll(labels, label =>
            {
                var row = MenuPalette.Item(label, enabled: false);
                menu.AddItem(row);
                return row;
            });
        }

        internal event Action<ExamOrders, bool> DischargeRequested;
        internal event Action<ExamOrders> PatientInteraction;


        internal event Action LieDownRequested;
        internal event Action ChairRequested;
        internal event Action<InpatientUnit> AdmissionRequested;

        internal bool IsOpen => Array.Exists(views, view => view.Visible);
        internal bool IsInspecting => inspectionMenu.Visible || inpatientInspection.Visible;
        internal bool IsCheckingVitals => vitalsMenu.Visible || inpatientVitals.Visible;

        internal void Open()
        {
            PatientInteraction?.Invoke(currentOrders);
            root.Visible = true;
        }

        internal void OpenExamination()
        {
            PatientInteraction?.Invoke(currentOrders);
            examination.Visible = true;
        }

        internal void OpenInpatient(string room)
        {
            PatientInteraction?.Invoke(currentOrders);
            inpatient.SubtitleText = room;
            inpatient.Visible = true;
        }

        internal void SetConsultationRoom(string roomName)
        {
            root.SubtitleText = roomName;
        }

        internal void Close()
        {
            foreach (UIMenu view in views)
            {
                view.Visible = false;
            }
        }

        internal void SetPatient(string name, DiseaseDefinition disease, Random random, ExamOrders orders)
        {
            patientName = name;
            currentOrders = orders;
            inpatient.SubtitleText = orders.Room;
            foreach (UIMenu conversationMenu in new[] { conversation, inpatientConversation })
            {
                conversationMenu.Clear();
            }
            AddConversationQuestions(disease, random, 0, orders.HasStartedConversation ? disease.Prompts.Count : 1);
            foreach (UIMenu view in views)
            {
                view.TitleText = name;
            }
        }

        private void AddConversationQuestions(DiseaseDefinition disease, Random random, int first, int end)
        {
            foreach (UIMenu conversationMenu in new[] { conversation, inpatientConversation })
            {
                for (int i = first; i < end; i++)
                {
                    int questionIndex = i;
                    string prompt = disease.Prompts[i];
                    var item = MenuPalette.Item(prompt);
                    conversationMenu.AddItem(item);
                    item.Activated += (sender, selectedItem) =>
                    {
                        if (canTalk())
                        {
                            PatientInteraction?.Invoke(currentOrders);
                            bool wasStarted = currentOrders.HasStartedConversation;
                            string answer = currentOrders.GetSymptomAnswer(disease, questionIndex, severityIndex, random);
                            Game.DisplaySubtitle(patientName + ": " + answer, 7000);
                            Game.LogTrivial("[Lumen Santé] " + patientName + " respondeu: " + prompt);
                            if (!wasStarted && currentOrders.HasStartedConversation)
                                AddConversationQuestions(disease, random, 1, disease.Prompts.Count);
                        }
                    };
                }
            }
        }

        internal void UpdateVitals(DiseaseStage stage, int index,
            (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) vitals)
        {
            severityIndex = index;
            string temperature = vitals.Temperature.HasValue
                ? vitals.Temperature.Value.ToString("F1", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " °C"
                : "Indisponível";
            foreach (UIMenuItem[] rows in inspectionRows)
            {
                rows[0].RightLabel = stage.Inspection.General;
                rows[1].RightLabel = stage.Inspection.Breathing;
                rows[2].RightLabel = stage.Inspection.Skin;
                rows[3].RightLabel = temperature;
            }

            string pulse = vitals.Pulse + " bpm";
            string pressure = vitals.Systolic + "/" + vitals.Diastolic + " mmHg";
            string breathing = vitals.RespiratoryRate + " irpm";
            string saturation = vitals.OxygenSaturation + "%";
            foreach (UIMenuItem[] rows in vitalsRows)
            {
                rows[0].RightLabel = pulse;
                rows[1].RightLabel = pressure;
                rows[2].RightLabel = breathing;
                rows[3].RightLabel = saturation;
                rows[4].RightLabel = temperature;
            }
        }
    }
}
