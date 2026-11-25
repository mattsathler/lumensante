using System;

namespace lumen_sante.Clinical
{
    using System.Linq;
    internal enum ExamStatus
    {
        Available,
        Pending,
        InProgress,
        Processing,
        Completed
    }

    internal enum ExamKind { Mri, XRay, Laboratory, Bedside }
    internal enum InpatientUnit { Room, Ward }

    internal sealed partial class ExamOrders
    {
        private readonly System.Collections.Generic.List<CareDecision> careHistory = new System.Collections.Generic.List<CareDecision>();
        internal System.Collections.Generic.IReadOnlyList<CareDecision> CareHistory => careHistory.AsReadOnly();
        internal long CareRevision { get; private set; }
        private long lastExamSequence;
        internal bool IsCareProtected { get; private set; }
        internal WorkerDiagnosis WorkerDiagnosis { get; } = new WorkerDiagnosis();
        internal PatientClinicalState ClinicalState { get; } = new PatientClinicalState();
        internal void RecordWorkerAssessment(CareAuthor author, long now, string hypothesis)
        {
            if (author == null || author.IsPlayer) throw new ArgumentException("Avaliação exige um Doctor.");
            if (!IsDischarged) RecordCare(author, now, "Hipótese diagnóstica", hypothesis ?? "Diagnóstico em aberto", false);
        }
        internal void RecordCoronerRequest(CareAuthor author, long now)
        {
            if (author == null) throw new ArgumentNullException(nameof(author));
            if (!IsDischarged) RecordCare(author, now, "Coroners", "Recolhimento solicitado", false);
        }
        internal void RecordNurseProcedure(CareAuthor nurse, long now, string procedure)
        {
            if (nurse == null || nurse.IsPlayer || string.IsNullOrWhiteSpace(procedure))
                throw new ArgumentException("Procedimento de enfermagem inválido.");
            if (!IsDischarged) RecordCare(nurse, now, "Procedimento de enfermagem", procedure, false);
        }

        internal bool CanApplyCare(CareAuthor author, long expectedRevision)
        {
            if (author == null) throw new ArgumentNullException(nameof(author));
            return !IsDischarged && expectedRevision == CareRevision && (author.IsPlayer || !IsCareProtected);
        }

        internal void RegisterPlayerInteraction(long now)
        {
            if (!IsDischarged) RecordCare(CareAuthor.Player, now, "Avaliação", "Atendimento assumido pelo jogador", false);
        }

        private void RecordCare(CareAuthor author, long now, string action, string detail, bool protectsPlan)
        {
            CareRevision++;
            if (protectsPlan && author.IsPlayer) IsCareProtected = true;
            careHistory.Add(new CareDecision(CareRevision, now, author, action, detail));
        }

        internal ExamOrders(string patientName, string room, string diseaseId, PatientProfile profile)
        {
            AdmissionId = Guid.NewGuid();
            PatientName = patientName;
            Room = room;
            DiseaseId = diseaseId;
            Profile = profile;
        }

        internal Guid AdmissionId { get; private set; }
        internal string PatientName { get; }
        internal string Room { get; private set; }
        internal bool IsInpatient { get; private set; }
        internal void AdmitTo(string room)
        {
            Room = room;
            IsInpatient = true;
        }
        internal string DiseaseId { get; }
        internal PatientProfile Profile { get; }
        internal string Notes { get; set; } = string.Empty;
        private readonly System.Collections.Generic.Dictionary<(int Stage, int Question), string> symptomAnswers =
            new System.Collections.Generic.Dictionary<(int Stage, int Question), string>();
        private readonly System.Collections.Generic.Dictionary<int, string> latestSymptomAnswers =
            new System.Collections.Generic.Dictionary<int, string>();
        internal bool HasStartedConversation { get; private set; }
        private long symptomRevision;
        internal long SymptomRevision => symptomRevision + ClinicalState.SymptomRevision;
        internal bool HasSymptomAnswer(int question) => latestSymptomAnswers.ContainsKey(question);

        internal void RecordInitialComplaint(DiseaseDefinition disease, int stage, Random random)
        {
            var key = (stage, 0);
            if (symptomAnswers.ContainsKey(key)) return;
            string answer = disease.PickAnswer(0, stage, random);
            symptomAnswers.Add(key, answer);
            latestSymptomAnswers[0] = answer;
            symptomRevision++;
        }

        internal System.Collections.Generic.IEnumerable<(string Prompt, string Answer)> SymptomHistory(DiseaseDefinition disease) =>
            latestSymptomAnswers.OrderBy(entry => entry.Key)
                .Select(entry => (entry.Key >= SymptomCatalog.InterviewIndexOffset
                    ? SymptomCatalog.InterviewPrompt(disease, entry.Key) ?? "Sintoma"
                    : entry.Key >= 100 ? SymptomCatalog.LegacyInterviewPrompt(entry.Key)
                    : entry.Key == 0 ? "Queixa inicial"
                    : disease != null && entry.Key < disease.Prompts.Count ? disease.Prompts[entry.Key] : "Sintoma",
                    entry.Value))
                .Concat(disease != null && disease.SymptomIds.Contains("breathlessness") && ClinicalState.HasVitals
                    ? new[] { ("Respiração — estado atual", ClinicalState.BreathlessnessDescription) }
                    : Array.Empty<(string, string)>())
                .Concat(ClinicalState.PainIntensity.HasValue
                    ? new[] { ("Dor — estado atual", PatientClinicalState.DescribeIntensity(ClinicalState.PainIntensity.Value)) }
                    : Array.Empty<(string, string)>())
                .Concat(ClinicalState.NauseaIntensity.HasValue
                    ? new[] { ("Náusea — estado atual", PatientClinicalState.DescribeIntensity(ClinicalState.NauseaIntensity.Value)) }
                    : Array.Empty<(string, string)>())
                .Concat(ClinicalState.NasalCongestionIntensity.HasValue
                    ? new[] { ("Congestão nasal — estado atual", PatientClinicalState.DescribeIntensity(ClinicalState.NasalCongestionIntensity.Value)) }
                    : Array.Empty<(string, string)>());

        internal string GetSymptomAnswer(DiseaseDefinition disease, int question, int stage, Random random)
        {
            if (IsRecovered)
            {
                if (question == SymptomCatalog.HelpQuestion) return "Estou me sentindo bem agora.";
                if (question == 0) HasStartedConversation = true;
                if (question >= SymptomCatalog.InterviewIndexOffset)
                    return SymptomCatalog.InterviewAnswer(disease, ClinicalState, question, stage, random);
                return "Estou me sentindo bem agora.";
            }
            var key = (stage, question);
            if (!symptomAnswers.TryGetValue(key, out string answer))
            {
                answer = question >= SymptomCatalog.InterviewIndexOffset
                    ? SymptomCatalog.InterviewAnswer(disease, ClinicalState, question, stage, random)
                    : disease.PickAnswer(question, stage, random);
                symptomAnswers.Add(key, answer);
            }
            if (!latestSymptomAnswers.TryGetValue(question, out string latest) || latest != answer)
            {
                latestSymptomAnswers[question] = answer;
                symptomRevision++;
            }
            if (question == SymptomCatalog.HelpQuestion || question == 0) HasStartedConversation = true;
            return answer;
        }
        internal bool IsRecovered { get; private set; }
        internal bool IsDischarged { get; private set; }
        internal int VisitCount { get; private set; } = 1;
        internal long LastDischargedAt { get; private set; }
        internal bool Discharge(long now, CareAuthor author, long expectedRevision)
        {
            if (!CanApplyCare(author, expectedRevision)) return false;
            IsDischarged = true;
            IsInpatient = false;
            LastDischargedAt = now;
            Room = "Fora do hospital";
            RecordCare(author, now, "Alta", "Alta hospitalar", false);
            return true;
        }
        internal void Readmit(string room)
        {
            if (IsDischarged) VisitCount++;
            IsDischarged = false;
            IsInpatient = false;
            Room = room;
        }

        internal void WaitForFollowUp() => Room = "Recepção";
        private readonly System.Collections.Generic.Dictionary<MedicationClass, long> medications =
            new System.Collections.Generic.Dictionary<MedicationClass, long>();

        internal bool PrescribeMedication(MedicationClass medication, long expiresAt,
            CareAuthor author, long now, long expectedRevision)
        {
            if (!Enum.IsDefined(typeof(MedicationClass), medication) || expiresAt <= now)
                throw new ArgumentException("Medicação ou prazo inválido.");
            if (IsRecovered || !CanApplyCare(author, expectedRevision)) return false;
            medications[medication] = expiresAt;
            RecordCare(author, now, "Prescrição", MedicationCatalog.Options.First(option => option.Class == medication).Name, true);
            return true;
        }
        internal bool StopMedication(MedicationClass medication, CareAuthor author, long now, long expectedRevision)
        {
            if (!Enum.IsDefined(typeof(MedicationClass), medication)) throw new ArgumentOutOfRangeException(nameof(medication));
            if (!CanApplyCare(author, expectedRevision)) return false;
            if (!medications.Remove(medication)) return false;
            RecordCare(author, now, "Retirada de medicação", MedicationCatalog.Options.First(option => option.Class == medication).Name, true);
            return true;
        }

        internal MedicationClass[] ActiveMedications(long now) => medications
            .Where(entry => entry.Value > now).Select(entry => entry.Key).ToArray();

        internal void RecordExamCompletion(object completedExam)
        {
            var exam = completedExam as ExamOrder;
            if (exam == null || !Items.Contains(exam) || exam.Status != ExamStatus.Completed)
                throw new ArgumentException("Resultado de exame inválido.", nameof(completedExam));
            exam.SetSequence(++lastExamSequence);
        }

        internal void MarkRecovered() => IsRecovered = true;
        internal string TreatmentDiseaseId { get; private set; }
        internal string DiagnosisDiseaseId { get; private set; }
        internal bool SelectTreatment(string diseaseId, CareAuthor author, long now, long expectedRevision)
        {
            if (string.IsNullOrWhiteSpace(diseaseId)) throw new ArgumentException("Tratamento sem ID.", nameof(diseaseId));
            if (IsRecovered || !CanApplyCare(author, expectedRevision)) return false;
            // Confirming an NPC's choice also transfers authority to the player.
            if (TreatmentDiseaseId == diseaseId && (!author.IsPlayer || IsCareProtected)) return false;
            TreatmentDiseaseId = diseaseId;
            RecordCare(author, now, "Tratamento", DiseaseCatalog.Find(diseaseId)?.TreatmentLabel ?? diseaseId, true);
            return true;
        }

        internal bool ClearTreatment(CareAuthor author, long now, long expectedRevision)
        {
            if (!CanApplyCare(author, expectedRevision) || TreatmentDiseaseId == null) return false;
            TreatmentDiseaseId = null;
            RecordCare(author, now, "Retirada de tratamento", "Tratamento específico suspenso", true);
            return true;
        }

        internal bool SetDiagnosis(string diseaseId, System.Collections.Generic.IReadOnlyDictionary<string, double> measurements,
            CareAuthor author, long now, long expectedRevision)
        {
            if (!CanApplyCare(author, expectedRevision)) return false;
            DiseaseDefinition disease = diseaseId == null ? null : DiseaseCatalog.Find(diseaseId);
            if (diseaseId != null && (disease == null || DiagnosticInvestigation.Excluded(disease, measurements))) return false;
            if (DiagnosisDiseaseId == diseaseId) return false;
            DiagnosisDiseaseId = diseaseId;
            RecordCare(author, now, disease == null ? "Diagnóstico removido" : "Diagnóstico",
                disease?.Name ?? "Hipótese diagnóstica retirada", true);
            return true;
        }

        internal ExamOrder[] Items { get; } =
        {
            new ExamOrder("mri", "MRI de tórax", 30 * 60000, ExamKind.Mri),
            new ExamOrder("mri-brain", "MRI de crânio", 30 * 60000, ExamKind.Mri),
            new ExamOrder("mri-spine", "MRI de coluna", 40 * 60000, ExamKind.Mri),
            new ExamOrder("x-ray", "RX de tórax", 10 * 60000, ExamKind.XRay),
            new ExamOrder("x-ray-ribs", "RX de costelas", 10 * 60000, ExamKind.XRay),
            new ExamOrder("x-ray-pelvis", "RX de pelve", 10 * 60000, ExamKind.XRay),
            new ExamOrder("x-ray-limbs", "RX de membros", 15 * 60000, ExamKind.XRay),
            new ExamOrder("cardiac", "Exames cardíacos", 15 * 60000, ExamKind.Bedside),
            new ExamOrder("vital-signs", "Sinais vitais", 2 * 60000, ExamKind.Bedside,
                "Avaliação de sinais vitais", "Medição clínica"),
            new ExamOrder("hematology", "Painel hematológico", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("renal", "Função renal", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("hepatic", "Função hepática", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("electrolytes", "Sódio sérico", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("blood-gas", "Gasometria arterial", 15 * 60000, ExamKind.Laboratory),
            new ExamOrder("coagulation", "INR (coagulação)", 25 * 60000, ExamKind.Laboratory),
            new ExamOrder("glucose", "Glicemia casual", 15 * 60000, ExamKind.Laboratory),
            new ExamOrder("crp", "Proteína C reativa (PCR)", 25 * 60000, ExamKind.Laboratory),
            new ExamOrder("urinalysis", "Urina: células", 25 * 60000, ExamKind.Laboratory),
            new ExamOrder("mri-abdomen", "MRI de abdome", 35 * 60000, ExamKind.Mri,
                normalFindings: "Órgãos abdominais sem lesões focais identificadas."),
            new ExamOrder("mri-pelvis", "MRI de pelve", 35 * 60000, ExamKind.Mri,
                normalFindings: "Estruturas pélvicas sem alterações focais identificadas."),
            new ExamOrder("mri-liver", "MRI de fígado", 35 * 60000, ExamKind.Mri,
                normalFindings: "Parênquima hepático sem lesões focais identificadas."),
            new ExamOrder("mri-kidneys", "MRI de rins", 35 * 60000, ExamKind.Mri,
                normalFindings: "Rins sem lesões focais ou dilatação das vias coletoras."),
            new ExamOrder("mri-shoulder", "MRI de ombro", 30 * 60000, ExamKind.Mri,
                normalFindings: "Manguito rotador sem roturas identificadas; sem derrame relevante."),
            new ExamOrder("mri-knee", "MRI de joelho", 30 * 60000, ExamKind.Mri,
                normalFindings: "Meniscos e ligamentos sem lesões identificadas."),
            new ExamOrder("mri-hip", "MRI de quadril", 30 * 60000, ExamKind.Mri,
                normalFindings: "Articulação do quadril sem alterações focais identificadas."),
            new ExamOrder("mri-cervical", "MRI de coluna cervical", 35 * 60000, ExamKind.Mri,
                normalFindings: "Canal cervical sem compressão medular ou protrusões relevantes."),
            new ExamOrder("mri-lumbar", "MRI de coluna lombar", 35 * 60000, ExamKind.Mri,
                normalFindings: "Canal lombar sem estenose ou compressão radicular identificada."),
            new ExamOrder("x-ray-skull", "RX de crânio", 10 * 60000, ExamKind.XRay,
                normalFindings: "Calota craniana sem fraturas identificadas."),
            new ExamOrder("x-ray-cervical", "RX de coluna cervical", 10 * 60000, ExamKind.XRay,
                normalFindings: "Alinhamento cervical preservado, sem fraturas identificadas."),
            new ExamOrder("x-ray-thoracic", "RX de coluna torácica", 10 * 60000, ExamKind.XRay,
                normalFindings: "Corpos vertebrais torácicos com alturas preservadas."),
            new ExamOrder("x-ray-lumbar", "RX de coluna lombar", 10 * 60000, ExamKind.XRay,
                normalFindings: "Alinhamento lombar preservado, sem fraturas identificadas."),
            new ExamOrder("x-ray-shoulder", "RX de ombro", 10 * 60000, ExamKind.XRay,
                normalFindings: "Articulação do ombro sem fratura ou luxação identificada."),
            new ExamOrder("x-ray-knee", "RX de joelho", 10 * 60000, ExamKind.XRay,
                normalFindings: "Espaços articulares do joelho preservados, sem fratura identificada."),
            new ExamOrder("x-ray-ankle", "RX de tornozelo", 10 * 60000, ExamKind.XRay,
                normalFindings: "Alinhamento do tornozelo preservado, sem fratura identificada."),
            new ExamOrder("calcium", "Cálcio total", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Cálcio total dentro do intervalo de referência."),
            new ExamOrder("magnesium", "Magnésio", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Magnésio dentro do intervalo de referência."),
            new ExamOrder("phosphate", "Fósforo", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Fósforo dentro do intervalo de referência."),
            new ExamOrder("urea", "Nitrogênio ureico", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Ureia dentro do intervalo de referência."),
            new ExamOrder("creatinine", "Creatinina", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Creatinina dentro do intervalo de referência."),
            new ExamOrder("albumin", "Albumina", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Albumina dentro do intervalo de referência."),
            new ExamOrder("bilirubin", "Bilirrubina total", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Bilirrubinas dentro dos intervalos de referência."),
            new ExamOrder("tsh", "TSH", 30 * 60000, ExamKind.Laboratory,
                "Imunoensaio", "Soro", "TSH dentro do intervalo de referência."),
            new ExamOrder("free-t4", "T4 livre", 30 * 60000, ExamKind.Laboratory,
                "Imunoensaio", "Soro", "T4 livre dentro do intervalo de referência."),
            new ExamOrder("ferritin", "Ferritina", 30 * 60000, ExamKind.Laboratory,
                "Imunoensaio", "Soro", "Ferritina dentro do intervalo de referência."),
            new ExamOrder("iron", "Ferro sérico", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Ferro sérico dentro do intervalo de referência."),
            new ExamOrder("lipid-profile", "Perfil lipídico", 25 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Colesterol e triglicerídeos sem elevação relevante."),
            new ExamOrder("hba1c", "Hemoglobina glicada", 30 * 60000, ExamKind.Laboratory,
                "Cromatografia", "Sangue total", "Hemoglobina glicada dentro do intervalo de referência."),
            new ExamOrder("lactate", "Lactato", 15 * 60000, ExamKind.Laboratory,
                "Análise enzimática", "Plasma", "Lactato dentro do intervalo de referência."),
            new ExamOrder("troponin", "Troponina", 25 * 60000, ExamKind.Laboratory,
                "Imunoensaio", "Plasma", "Troponina sem elevação relevante."),
            new ExamOrder("bnp", "Peptídeo natriurético (BNP)", 30 * 60000, ExamKind.Laboratory,
                "Imunoensaio", "Plasma", "BNP sem elevação relevante."),
            new ExamOrder("urine-culture", "Urocultura", 45 * 60000, ExamKind.Laboratory,
                "Cultura microbiológica", "Urina", "Sem crescimento bacteriano na amostra de urina."),
            new ExamOrder("urine-pbg", "Porfobilinogênio urinário", 45 * 60000, ExamKind.Laboratory,
                "Análise quantitativa", "Urina"),
            new ExamOrder("blood-culture", "Hemocultura", 60 * 60000, ExamKind.Laboratory,
                "Cultura microbiológica", "Sangue", "Sem crescimento microbiano na amostra de sangue.")
        };
    }

    internal sealed class ExamOrder
    {
        private readonly System.Collections.Generic.List<ExamOrder> previousResults = new System.Collections.Generic.List<ExamOrder>();
        internal System.Collections.Generic.IEnumerable<ExamOrder> Results => previousResults.Concat(
            Status == ExamStatus.Completed ? new[] { this } : Array.Empty<ExamOrder>());
        internal ExamOrder(string id, string name, int durationMilliseconds, ExamKind kind,
            string method = null, string sample = null, string normalFindings = null)
        {
            Id = id;
            Name = name;
            DurationMilliseconds = durationMilliseconds;
            Kind = kind;
            Method = method;
            Sample = sample;
            NormalFindings = normalFindings;
        }

        internal string Id { get; }
        internal string Method { get; }
        internal string Sample { get; }
        internal string NormalFindings { get; }
        internal string Name { get; }
        internal int DurationMilliseconds { get; }
        internal ExamKind Kind { get; }
        internal ExamStatus Status { get; private set; }
        internal int StageAtRequest { get; private set; }
        internal long CompletedAtMilliseconds { get; private set; }
        internal long Sequence { get; private set; }
        internal bool RequestedByPlayer { get; private set; }
        internal string RequestedBy { get; private set; }
        internal string PerformedBy { get; private set; }
        internal string SavedFindings { get; private set; }
        internal bool CanRepeat => ClinicalParameterCatalog.ForExam(Id).Count > 0;
        internal ExamOrders.SavedMeasurement[] Measurements { get; private set; }

        internal void Restore(ExamOrders.SavedExam data)
        {
            if (Status == ExamStatus.Completed) ArchiveCurrent();
            Status = ExamStatus.Completed;
            StageAtRequest = data.Stage;
            CompletedAtMilliseconds = data.At;
            Sequence = data.Sequence;
            RequestedBy = data.Requester;
            RequestedByPlayer = data.Requester == "Jogador";
            PerformedBy = data.Performer;
            SavedFindings = data.Findings;
            Measurements = data.Measurements;
        }

        internal bool TryRequest(int stage)
        {
            if (Status != ExamStatus.Available && !(CanRepeat && Status == ExamStatus.Completed))
            {
                return false;
            }

            if (Status == ExamStatus.Completed) ArchiveCurrent();

            StageAtRequest = stage;
            SavedFindings = null;
            Measurements = null;
            CompletedAtMilliseconds = 0;
            Sequence = 0;
            RequestedByPlayer = true;
            RequestedBy = "Jogador";
            PerformedBy = null;
            Status = ExamStatus.Pending;
            return true;
        }

        private void ArchiveCurrent()
        {
            var copy = new ExamOrder(Id, Name, DurationMilliseconds, Kind, Method, Sample, NormalFindings);
            copy.Restore(new ExamOrders.SavedExam
            {
                Id = Id, Stage = StageAtRequest, At = CompletedAtMilliseconds,
                Requester = RequestedBy, Performer = PerformedBy, Findings = SavedFindings,
                Measurements = Measurements, Sequence = Sequence
            });
            previousResults.Add(copy);
        }

        internal bool TryRequestByNamedWorker(int stage, CareAuthor doctor)
        {
            if (!TryRequest(stage)) return false;
            RequestedByPlayer = false;
            RequestedBy = doctor.Name;
            return true;
        }

        internal bool TryRequestByWorker(int stage) => TryRequestByNamedWorker(stage, CareAuthor.Doctor("unknown", "Doctor"));

        internal void RecordPerformer(string name)
        {
            if (Status == ExamStatus.Completed) PerformedBy = name;
        }

        internal void SetSequence(long sequence)
        {
            if (Status == ExamStatus.Completed && sequence > 0) Sequence = sequence;
        }

        internal void RecordFindings(string findings)
        {
            if (Status == ExamStatus.Completed) SavedFindings = findings;
        }

        internal void RecordMeasurements(ExamOrders.SavedMeasurement[] measurements)
        {
            if (Status == ExamStatus.Completed) Measurements = measurements;
        }

        internal bool TryComplete(long elapsedMilliseconds)
        {
            if (Status != ExamStatus.InProgress && Status != ExamStatus.Processing)
            {
                return false;
            }

            CompletedAtMilliseconds = elapsedMilliseconds;
            Status = ExamStatus.Completed;
            return true;
        }

        internal bool TrySetStatus(ExamStatus status)
        {
            if (Status == status || Status == ExamStatus.Completed || Status == ExamStatus.Available)
            {
                return false;
            }

            Status = status;
            return true;
        }

        internal void Cancel()
        {
            if (Status == ExamStatus.Pending || Status == ExamStatus.InProgress || Status == ExamStatus.Processing)
            {
                Status = ExamStatus.Available;
            }
        }
    }
}
