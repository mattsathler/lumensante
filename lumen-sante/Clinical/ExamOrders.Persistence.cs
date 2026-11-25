using System;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed partial class ExamOrders
    {
        public sealed class Snapshot
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Disease { get; set; }
            public string Notes { get; set; }
            public int Age { get; set; }
            public string Phone { get; set; }
            public string BloodType { get; set; }
            public string[] PriorExams { get; set; }
            public int[] PriorExamMonths { get; set; }
            public int Visits { get; set; }
            public bool Recovered { get; set; }
            public bool Discharged { get; set; }
            public bool Protected { get; set; }
            public bool Conversation { get; set; }
            public long Revision { get; set; }
            public string Treatment { get; set; }
            public string Diagnosis { get; set; }
            public SavedAnswer[] Symptoms { get; set; }
            public SavedAnswer[] Answers { get; set; }
            public SavedCare[] Care { get; set; }
            public SavedExam[] Exams { get; set; }
            public SavedMedication[] Medications { get; set; }
            public SavedVitals Vitals { get; set; }
            public int? Leukocytes { get; set; }
            public int NaturalRecoveryProgress { get; set; }
            public int? Pain { get; set; }
            public int? Nausea { get; set; }
            public int? NasalCongestion { get; set; }
            public SavedParameter[] Chemistry { get; set; }

            internal bool IsValid() => Id != Guid.Empty && !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(Disease) && Visits > 0 && NaturalRecoveryProgress >= 0 && NaturalRecoveryProgress <= 1000 &&
                (!Pain.HasValue || (Pain.Value >= 0 && Pain.Value <= 10)) &&
                (!Nausea.HasValue || (Nausea.Value >= 0 && Nausea.Value <= 10)) &&
                (!NasalCongestion.HasValue || (NasalCongestion.Value >= 0 && NasalCongestion.Value <= 10)) &&
                PriorExams != null && PriorExamMonths != null && PriorExams.Length == PriorExamMonths.Length &&
                Symptoms != null && Symptoms.All(item => item != null && item.Question >= 0) &&
                Answers != null && Answers.All(item => item != null && item.Stage >= 0 && item.Question >= 0) &&
                Care != null && Care.All(item => item != null && !string.IsNullOrWhiteSpace(item.AuthorId) &&
                    !string.IsNullOrWhiteSpace(item.AuthorName)) &&
                Exams != null && Exams.All(item => item != null && item.Stage >= 0 && item.Sequence >= 0 &&
                    (item.Measurements == null || item.Measurements.All(measurement => measurement != null &&
                        !string.IsNullOrWhiteSpace(measurement.Id) && !string.IsNullOrWhiteSpace(measurement.Name) &&
                        measurement.Unit != null && measurement.Decimals >= 0 && measurement.Decimals <= 6 &&
                        !double.IsNaN(measurement.Minimum) && !double.IsInfinity(measurement.Minimum) &&
                        !double.IsNaN(measurement.Maximum) && !double.IsInfinity(measurement.Maximum) &&
                        measurement.Minimum <= measurement.Maximum && (!measurement.Value.HasValue ||
                            (!double.IsNaN(measurement.Value.Value) && !double.IsInfinity(measurement.Value.Value)))))) &&
                Exams.Where(item => item.Sequence > 0).Select(item => item.Sequence).Distinct().Count() == Exams.Count(item => item.Sequence > 0) &&
                (Chemistry == null || Chemistry.All(item => item != null && !string.IsNullOrWhiteSpace(item.Id) &&
                    !double.IsNaN(item.Value) && !double.IsInfinity(item.Value)) && Chemistry.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == Chemistry.Length) &&
                Medications != null && Medications.All(item => item != null && item.Expires <= DateTime.MaxValue.Ticks &&
                    Enum.IsDefined(typeof(MedicationClass), item.Class));
        }

        public sealed class SavedAnswer
        {
            public int Stage { get; set; }
            public int Question { get; set; }
            public string Text { get; set; }
        }

        public sealed class SavedCare
        {
            public long Revision { get; set; }
            public long At { get; set; }
            public string AuthorId { get; set; }
            public string AuthorName { get; set; }
            public string Action { get; set; }
            public string Detail { get; set; }
        }

        public sealed class SavedExam
        {
            public string Id { get; set; }
            public int Stage { get; set; }
            public long At { get; set; }
            public long Sequence { get; set; }
            public string Requester { get; set; }
            public string Performer { get; set; }
            public string Findings { get; set; }
            public SavedMeasurement[] Measurements { get; set; }
        }

        public sealed class SavedMeasurement
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public double Minimum { get; set; }
            public double Maximum { get; set; }
            public int Decimals { get; set; }
            public double? Value { get; set; }
        }

        public sealed class SavedMedication
        {
            public MedicationClass Class { get; set; }
            public long Expires { get; set; }
        }

        public sealed class SavedParameter
        {
            public string Id { get; set; }
            public double Value { get; set; }
        }

        public sealed class SavedVitals
        {
            public int Pulse { get; set; }
            public int Systolic { get; set; }
            public int Diastolic { get; set; }
            public int RespiratoryRate { get; set; }
            public int OxygenSaturation { get; set; }
            public double? Temperature { get; set; }
        }

        internal Snapshot Capture(long now, DateTime date) => new Snapshot
        {
            Id = AdmissionId, Name = PatientName, Disease = DiseaseId, Notes = Notes, Leukocytes = ClinicalState.Leukocytes,
            NaturalRecoveryProgress = ClinicalState.NaturalRecoveryProgress,
            Pain = ClinicalState.PainIntensity, Nausea = ClinicalState.NauseaIntensity, NasalCongestion = ClinicalState.NasalCongestionIntensity,
            Chemistry = ClinicalState.Chemistry.Select(item => new SavedParameter { Id = item.Key, Value = item.Value }).ToArray(),
            Vitals = ClinicalState.HasVitals ? new SavedVitals
            {
                Pulse = ClinicalState.Vitals.Pulse, Systolic = ClinicalState.Vitals.Systolic,
                Diastolic = ClinicalState.Vitals.Diastolic, RespiratoryRate = ClinicalState.Vitals.RespiratoryRate,
                OxygenSaturation = ClinicalState.Vitals.OxygenSaturation, Temperature = ClinicalState.Vitals.Temperature
            } : null,
            Age = Profile.Age, Phone = Profile.Phone, BloodType = Profile.BloodType,
            PriorExams = Profile.MedicalHistory.Select(item => item.Name).ToArray(),
            PriorExamMonths = Profile.MedicalHistory.Select(item => item.MonthsAgo).ToArray(),
            Visits = VisitCount, Recovered = IsRecovered, Discharged = IsDischarged, Protected = IsCareProtected,
            Conversation = HasStartedConversation, Revision = CareRevision, Treatment = TreatmentDiseaseId,
            Diagnosis = DiagnosisDiseaseId,
            Symptoms = latestSymptomAnswers.Select(item => new SavedAnswer { Question = item.Key, Text = item.Value }).ToArray(),
            Answers = symptomAnswers.Select(item => new SavedAnswer
                { Stage = item.Key.Stage, Question = item.Key.Question, Text = item.Value }).ToArray(),
            Care = careHistory.Select(item => new SavedCare
            {
                Revision = item.Revision, At = item.At, AuthorId = item.Author.Id,
                AuthorName = item.Author.Name, Action = item.Action, Detail = item.Detail
            }).ToArray(),
            Exams = Items.SelectMany(item => item.Results).Select(item => new SavedExam
            {
                Id = item.Id, Stage = item.StageAtRequest, At = item.CompletedAtMilliseconds,
                Sequence = item.Sequence,
                Requester = item.RequestedBy, Performer = item.PerformedBy, Findings = ExamFindings.Describe(this, item),
                Measurements = item.Measurements
            }).ToArray(),
            Medications = medications.Where(item => item.Value > now).Select(item => new SavedMedication
            {
                Class = item.Key, Expires = date.AddMilliseconds(item.Value - now).Ticks
            }).ToArray()
        };

        internal static ExamOrders Restore(Snapshot data, long now, DateTime date)
        {
            var profile = new PatientProfile(data.Age, data.Phone, data.BloodType,
                data.PriorExams.Select((name, index) => new PreviousExam(name, data.PriorExamMonths[index])).ToArray());
            var orders = new ExamOrders(data.Name, "Fora do hospital", data.Disease, profile)
            {
                AdmissionId = data.Id, Notes = data.Notes ?? string.Empty, VisitCount = data.Visits,
                IsDischarged = data.Discharged, IsRecovered = data.Recovered, IsCareProtected = data.Protected,
                HasStartedConversation = data.Conversation, CareRevision = data.Revision, TreatmentDiseaseId = data.Treatment,
                DiagnosisDiseaseId = data.Diagnosis
            };
            if (data.Vitals != null)
                orders.ClinicalState.SetVitals((data.Vitals.Pulse, data.Vitals.Systolic, data.Vitals.Diastolic,
                    data.Vitals.RespiratoryRate, data.Vitals.OxygenSaturation, data.Vitals.Temperature));
            if (data.Leukocytes.HasValue) orders.ClinicalState.SetLeukocytes(data.Leukocytes.Value);
            orders.ClinicalState.SetNaturalRecoveryProgress(data.NaturalRecoveryProgress);
            orders.ClinicalState.RestoreSymptoms(data.Pain, data.Nausea, data.NasalCongestion);
            if (data.Chemistry != null)
                foreach (var item in data.Chemistry) orders.ClinicalState.SetChemistry(item.Id, item.Value);
            foreach (var item in data.Symptoms) orders.latestSymptomAnswers[item.Question] = item.Text;
            foreach (var item in data.Answers) orders.symptomAnswers[(item.Stage, item.Question)] = item.Text;
            foreach (var item in data.Care)
            {
                CareAuthor author = item.AuthorId == CareAuthor.Player.Id ? CareAuthor.Player :
                    CareAuthor.Doctor(item.AuthorId, item.AuthorName);
                orders.careHistory.Add(new CareDecision(item.Revision, item.At, author, item.Action, item.Detail));
            }
            foreach (var item in data.Exams) orders.Items.FirstOrDefault(exam => exam.Id == item.Id)?.Restore(item);
            orders.lastExamSequence = Math.Max(0, data.Exams.Where(item => item.Sequence > 0).Select(item => item.Sequence).DefaultIfEmpty().Max());
            foreach (var item in data.Medications.Where(item => item.Expires > date.Ticks))
                orders.medications[item.Class] = now + (item.Expires - date.Ticks) / TimeSpan.TicksPerMillisecond;
            return orders;
        }
    }
}
