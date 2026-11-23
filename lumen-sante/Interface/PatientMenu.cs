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
        private readonly UIMenu examsMenu;
        private readonly UIMenu[] examGroups;
        private ExamOrders currentOrders;
        private UIMenuItem[] examItems = Array.Empty<UIMenuItem>();
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
            inspectionMenu = pool.AddSubMenu(examination, "Avaliação geral");
            vitalsMenu = pool.AddSubMenu(examination, "Sinais vitais");
            inpatientInspection = pool.AddSubMenu(inpatient, "Avaliação geral");
            inpatientVitals = pool.AddSubMenu(inpatient, "Sinais vitais");
            conversation = pool.AddSubMenu(root, "Conversa com Paciente");
            inpatientConversation = pool.AddSubMenu(inpatient, "Conversa com Paciente");
            examsMenu = pool.AddSubMenu(inpatient, "Exames");
            examGroups = new[]
            {
                pool.AddSubMenu(examsMenu, "Ressonância magnética (MRI)"),
                pool.AddSubMenu(examsMenu, "Radiografia (RX)"),
                pool.AddSubMenu(examsMenu, "Laboratório"),
                pool.AddSubMenu(examsMenu, "À beira do leito")
            };


            var lieDown = new UIMenuItem("Pedir para se deitar");
            root.AddItem(lieDown);
            var admissionMenu = pool.AddSubMenu(root, "Internar Paciente");
            var returnToChair = new UIMenuItem("Retornar paciente à cadeira");
            var commonBed = new UIMenuItem("Quarto");
            admissionMenu.AddItem(commonBed);
            commonBed.Activated += (sender, item) => AdmissionRequested?.Invoke(InpatientUnit.Room);
            var wardBed = new UIMenuItem("Enfermaria");
            admissionMenu.AddItem(wardBed);
            wardBed.Activated += (sender, item) => AdmissionRequested?.Invoke(InpatientUnit.Ward);
            examination.AddItem(returnToChair);
            admissionMenu.AddItem(new UIMenuItem("Emergência") { Enabled = false });
            lieDown.Activated += (sender, item) => LieDownRequested?.Invoke();
            returnToChair.Activated += (sender, item) => ChairRequested?.Invoke();
            var dischargeMenus = new[] { root, inpatient }.Select(parent =>
            {
                UIMenu confirmation = pool.AddSubMenu(parent, parent == root ? "Alta Hospitalar" : "Alta hospitalar");
                confirmation.SubtitleText = "Confirmar a alta deste paciente?";
                var followUp = new UIMenuCheckboxItem("Agendar retorno?", false);
                var confirm = new UIMenuItem("Dar Alta");
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
                inpatientVitals, conversation, inpatientConversation, examsMenu, admissionMenu }
                .Concat(examGroups).Concat(dischargeMenus).ToArray();

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
                var row = new UIMenuItem(label) { Enabled = false };
                menu.AddItem(row);
                return row;
            });
        }

        internal event Action<ExamOrder> ExamRequested;
        internal event Action<ExamOrders, bool> DischargeRequested;
        internal event Action<ExamOrders> PatientInteraction;


        internal event Action LieDownRequested;
        internal event Action ChairRequested;
        internal event Action<InpatientUnit> AdmissionRequested;

        internal bool IsOpen => Array.Exists(views, view => view.Visible);
        internal bool IsInspecting => inspectionMenu.Visible || inpatientInspection.Visible;
        internal bool IsCheckingVitals => vitalsMenu.Visible || inpatientVitals.Visible;
        internal bool IsChoosingExams => examsMenu.Visible || Array.Exists(examGroups, group => group.Visible);

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
            RefreshExamStatuses();
            inpatient.SubtitleText = room;
            inpatient.Visible = true;
        }

        private void RefreshExamStatuses()
        {
            if (currentOrders == null || examItems == null) return;
            for (int index = 0; index < currentOrders.Items.Length; index++)
                UpdateExamStatus(index, currentOrders.Items[index]);
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

        internal void UpdateExamStatus(int index, ExamOrder order)
        {
            UIMenuItem item = examItems[index];
            item.Enabled = order.Status == ExamStatus.Available;
            switch (order.Status)
            {
                case ExamStatus.Pending:
                    item.RightLabel = "Aguardando realização";
                    break;
                case ExamStatus.InProgress:
                    item.RightLabel = "Em andamento";
                    break;
                case ExamStatus.Processing:
                    item.RightLabel = "Processando";
                    break;
                case ExamStatus.Completed:
                    item.RightLabel = "Realizado";
                    break;
                default:
                    item.RightLabel = "A solicitar";
                    break;
            }
        }

        internal void SetPatient(string name, DiseaseDefinition disease, Random random, ExamOrders orders)
        {
            patientName = name;
            currentOrders = orders;
            inpatient.SubtitleText = orders.Room;
            foreach (UIMenu group in examGroups) group.Clear();
            examItems = new UIMenuItem[orders.Items.Length];
            foreach (int i in Enumerable.Range(0, orders.Items.Length).OrderBy(index => orders.Items[index].Name))
            {
                ExamOrder order = orders.Items[i];
                var item = new UIMenuItem(order.Name);
                examItems[i] = item;
                examGroups[(int)order.Kind].AddItem(item);
                UpdateExamStatus(i, order);
                item.Activated += (sender, selectedItem) => ExamRequested?.Invoke(order);
            }
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
                    var item = new UIMenuItem(prompt);
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
