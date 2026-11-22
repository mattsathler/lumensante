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

    internal sealed class ExamOrders
    {
        private readonly System.Collections.Generic.List<CareDecision> careHistory = new System.Collections.Generic.List<CareDecision>();
        internal System.Collections.Generic.IReadOnlyList<CareDecision> CareHistory => careHistory.AsReadOnly();
        internal long CareRevision { get; private set; }
        internal bool IsCareProtected { get; private set; }
        internal WorkerDiagnosis WorkerDiagnosis { get; } = new WorkerDiagnosis();
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

        internal Guid AdmissionId { get; }
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
        internal bool HasStartedConversation { get; private set; }

        internal string GetSymptomAnswer(DiseaseDefinition disease, int question, int stage, Random random)
        {
            if (IsRecovered)
            {
                if (question == 0) HasStartedConversation = true;
                return "Estou me sentindo bem agora.";
            }
            var key = (stage, question);
            if (!symptomAnswers.TryGetValue(key, out string answer))
            {
                answer = disease.PickAnswer(question, stage, random);
                symptomAnswers.Add(key, answer);
            }
            if (question == 0) HasStartedConversation = true;
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

        internal void MarkRecovered() => IsRecovered = true;
        internal string TreatmentDiseaseId { get; private set; }
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
            new ExamOrder("hematology", "Painel hematológico", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("renal", "Função renal", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("hepatic", "Função hepática", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("electrolytes", "Eletrólitos", 20 * 60000, ExamKind.Laboratory),
            new ExamOrder("blood-gas", "Gasometria arterial", 15 * 60000, ExamKind.Laboratory),
            new ExamOrder("coagulation", "Coagulograma", 25 * 60000, ExamKind.Laboratory),
            new ExamOrder("glucose", "Glicemia", 15 * 60000, ExamKind.Laboratory),
            new ExamOrder("crp", "Proteína C reativa (PCR)", 25 * 60000, ExamKind.Laboratory),
            new ExamOrder("urinalysis", "Urina tipo I", 25 * 60000, ExamKind.Laboratory),
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
            new ExamOrder("urea", "Ureia", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Ureia dentro do intervalo de referência."),
            new ExamOrder("creatinine", "Creatinina", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Creatinina dentro do intervalo de referência."),
            new ExamOrder("albumin", "Albumina", 20 * 60000, ExamKind.Laboratory,
                "Bioquímica automatizada", "Soro", "Albumina dentro do intervalo de referência."),
            new ExamOrder("bilirubin", "Bilirrubinas", 20 * 60000, ExamKind.Laboratory,
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
            new ExamOrder("blood-culture", "Hemocultura", 60 * 60000, ExamKind.Laboratory,
                "Cultura microbiológica", "Sangue", "Sem crescimento microbiano na amostra de sangue.")
        };
    }

    internal sealed class ExamOrder
    {
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
        internal bool RequestedByPlayer { get; private set; }

        internal bool TryRequest(int stage)
        {
            if (Status != ExamStatus.Available)
            {
                return false;
            }

            StageAtRequest = stage;
            RequestedByPlayer = true;
            Status = ExamStatus.Pending;
            return true;
        }

        internal bool TryRequestByWorker(int stage)
        {
            if (!TryRequest(stage)) return false;
            RequestedByPlayer = false;
            return true;
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
