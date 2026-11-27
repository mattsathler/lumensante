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
        private readonly UIMenu inpatientExams;
        private readonly UIMenu inpatientTransfer;
        private readonly UIMenu conversation;
        private readonly UIMenu inpatientConversation;
        private readonly Func<bool> canTalk;
        private ExamOrders currentOrders;
        private readonly UIMenu[] views;
        private string inspectionResult, vitalsResult;
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
            conversation = MenuPalette.SubMenu(pool, root, "Conversa com Paciente");
            inpatientConversation = MenuPalette.SubMenu(pool, inpatient, "Conversa com Paciente");
            inpatientExams = MenuPalette.SubMenu(pool, inpatient, "Exames");
            inpatientTransfer = MenuPalette.SubMenu(pool, inpatient, "Transferir Paciente");


            foreach (UIMenu examMenu in new[] { examination, inpatientExams })
            {
                var inspection = MenuPalette.Item("Avalia\u00e7\u00e3o geral");
                var vitals = MenuPalette.Item("Sinais vitais");
                examMenu.AddItem(inspection);
                examMenu.AddItem(vitals);
                inspection.Activated += (sender, item) => RequestBedsideExam(false);
                vitals.Activated += (sender, item) => RequestBedsideExam(true);
            }
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
                UIMenu confirmation = MenuPalette.SubMenu(pool, parent, "Alta Hospitalar", ActionTone.Accent);
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
            views = new[] { root, examination, inpatient, inpatientExams, inpatientTransfer, conversation, inpatientConversation, admissionMenu }
                .Concat(dischargeMenus).ToArray();

        }

        private void RequestBedsideExam(bool vitals)
        {
            if (!canTalk() || currentOrders == null) return;
            PatientInteraction?.Invoke(currentOrders);
            Close();
            BedsideExamRequested?.Invoke(currentOrders, vitals);
        }
        internal string BedsideResult(bool vitals) => vitals ? vitalsResult : inspectionResult;
        internal event Action<ExamOrders, bool> BedsideExamRequested;

        internal event Action<ExamOrders, bool> DischargeRequested;
        internal event Action<ExamOrders> PatientInteraction;


        internal event Action LieDownRequested;
        internal event Action ChairRequested;
        internal event Action<InpatientUnit> AdmissionRequested;
        internal event Action<InpatientUnit> TransferRequested;

        internal bool IsOpen => Array.Exists(views, view => view.Visible);

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

        internal void OpenInpatient(string room, InpatientUnit unit)
        {
            PatientInteraction?.Invoke(currentOrders);
            inpatient.SubtitleText = room;
            inpatientTransfer.Clear();
            InpatientUnit destination = unit == InpatientUnit.Ward ? InpatientUnit.Room : InpatientUnit.Ward;
            var transfer = MenuPalette.Item(destination == InpatientUnit.Room ? "Quarto" : "Enfermaria");
            inpatientTransfer.AddItem(transfer);
            transfer.Activated += (sender, item) => TransferRequested?.Invoke(destination);
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
            var interviewQuestions = SymptomCatalog.InterviewQuestions(disease);
            int firstQuestion = orders.HasSymptomAnswer(SymptomCatalog.HelpQuestion) ? 1 : 0;
            AddConversationQuestions(disease, random, firstQuestion,
                firstQuestion == 0 ? 1 : interviewQuestions.Length);
            foreach (UIMenu view in views)
            {
                view.TitleText = name;
            }
        }

        private void AddConversationQuestions(DiseaseDefinition disease, Random random, int first, int end)
        {
            var questions = SymptomCatalog.InterviewQuestions(disease);
            foreach (UIMenu conversationMenu in new[] { conversation, inpatientConversation })
            {
                for (int i = first; i < end; i++)
                {
                    int questionIndex = questions[i].Index;
                    string prompt = questions[i].Prompt;
                    var item = MenuPalette.Item(prompt);
                    conversationMenu.AddItem(item);
                    item.Activated += (sender, selectedItem) =>
                    {
                        if (canTalk())
                        {
                            PatientInteraction?.Invoke(currentOrders);
                            bool firstQuestion = !currentOrders.HasSymptomAnswer(SymptomCatalog.HelpQuestion);
                            string answer = currentOrders.GetSymptomAnswer(disease, questionIndex, severityIndex, random);
                            Game.DisplaySubtitle(patientName + ": " + answer, 7000);
                            Game.LogTrivial("[Lumen Santé] " + patientName + " respondeu: " + prompt);
                            if (questionIndex == SymptomCatalog.HelpQuestion && firstQuestion)
                                AddConversationQuestions(disease, random, 1, SymptomCatalog.InterviewQuestions(disease).Length);
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
            inspectionResult = "Estado geral: " + stage.Inspection.General + "~n~Respira\u00e7\u00e3o: " + stage.Inspection.Breathing +
                "~n~Pele e l\u00e1bios: " + stage.Inspection.Skin + "~n~Temperatura: " + temperature;
            vitalsResult = "Frequ\u00eancia card\u00edaca: " + vitals.Pulse + " bpm~n~Press\u00e3o arterial: " + vitals.Systolic + "/" + vitals.Diastolic +
                " mmHg~n~Frequ\u00eancia respirat\u00f3ria: " + vitals.RespiratoryRate + " irpm~n~Satura\u00e7\u00e3o: " + vitals.OxygenSaturation +
                "%~n~Temperatura: " + temperature;
        }
    }
}
