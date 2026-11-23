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
            public SavedAnswer[] Symptoms { get; set; }
            public SavedAnswer[] Answers { get; set; }
            public SavedCare[] Care { get; set; }
            public SavedExam[] Exams { get; set; }
            public SavedMedication[] Medications { get; set; }

            internal bool IsValid() => Id != Guid.Empty && !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(Disease) && Visits > 0 &&
                PriorExams != null && PriorExamMonths != null && PriorExams.Length == PriorExamMonths.Length &&
                Symptoms != null && Symptoms.All(item => item != null && item.Question >= 0) &&
                Answers != null && Answers.All(item => item != null && item.Stage >= 0 && item.Question >= 0) &&
                Care != null && Care.All(item => item != null && !string.IsNullOrWhiteSpace(item.AuthorId) &&
                    !string.IsNullOrWhiteSpace(item.AuthorName)) &&
                Exams != null && Exams.All(item => item != null && item.Stage >= 0) &&
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
            public string Requester { get; set; }
            public string Performer { get; set; }
            public string Findings { get; set; }
        }

        public sealed class SavedMedication
        {
            public MedicationClass Class { get; set; }
            public long Expires { get; set; }
        }

        internal Snapshot Capture(long now, DateTime date) => new Snapshot
        {
            Id = AdmissionId, Name = PatientName, Disease = DiseaseId, Notes = Notes,
            Age = Profile.Age, Phone = Profile.Phone, BloodType = Profile.BloodType,
            PriorExams = Profile.MedicalHistory.Select(item => item.Name).ToArray(),
            PriorExamMonths = Profile.MedicalHistory.Select(item => item.MonthsAgo).ToArray(),
            Visits = VisitCount, Recovered = IsRecovered, Discharged = IsDischarged, Protected = IsCareProtected,
            Conversation = HasStartedConversation, Revision = CareRevision, Treatment = TreatmentDiseaseId,
            Symptoms = latestSymptomAnswers.Select(item => new SavedAnswer { Question = item.Key, Text = item.Value }).ToArray(),
            Answers = symptomAnswers.Select(item => new SavedAnswer
                { Stage = item.Key.Stage, Question = item.Key.Question, Text = item.Value }).ToArray(),
            Care = careHistory.Select(item => new SavedCare
            {
                Revision = item.Revision, At = item.At, AuthorId = item.Author.Id,
                AuthorName = item.Author.Name, Action = item.Action, Detail = item.Detail
            }).ToArray(),
            Exams = Items.Where(item => item.Status == ExamStatus.Completed).Select(item => new SavedExam
            {
                Id = item.Id, Stage = item.StageAtRequest, At = item.CompletedAtMilliseconds,
                Requester = item.RequestedBy, Performer = item.PerformedBy, Findings = ExamFindings.Describe(this, item)
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
                HasStartedConversation = data.Conversation, CareRevision = data.Revision, TreatmentDiseaseId = data.Treatment
            };
            foreach (var item in data.Symptoms) orders.latestSymptomAnswers[item.Question] = item.Text;
            foreach (var item in data.Answers) orders.symptomAnswers[(item.Stage, item.Question)] = item.Text;
            foreach (var item in data.Care)
            {
                CareAuthor author = item.AuthorId == CareAuthor.Player.Id ? CareAuthor.Player :
                    CareAuthor.Doctor(item.AuthorId, item.AuthorName);
                orders.careHistory.Add(new CareDecision(item.Revision, item.At, author, item.Action, item.Detail));
            }
            foreach (var item in data.Exams) orders.Items.FirstOrDefault(exam => exam.Id == item.Id)?.Restore(item);
            foreach (var item in data.Medications.Where(item => item.Expires > date.Ticks))
                orders.medications[item.Class] = now + (item.Expires - date.Ticks) / TimeSpan.TicksPerMillisecond;
            return orders;
        }
    }
}
