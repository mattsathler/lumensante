using System;
using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed class WorkerDiagnosis
    {
        internal bool HasAssessment { get; private set; }
        internal DiseaseDefinition Hypothesis { get; private set; }
        internal bool HasConfirmingExam { get; private set; }
        private readonly HashSet<string> reviewedExams = new HashSet<string>(StringComparer.Ordinal);

        internal bool Assess(WorkerDiagnosticRules rules, DiseaseDefinition actual, IEnumerable<ExamOrder> exams, Random random)
        {
            if (!HasAssessment)
            {
                HasAssessment = true;
                Hypothesis = rules.Choose(actual, random, 0);
                return true;
            }
            bool changedEvidence = false;
            foreach (ExamOrder exam in exams)
            {
                if (exam.Status != ExamStatus.Completed || reviewedExams.Contains(exam.Id)) continue;
                reviewedExams.Add(exam.Id);
                if (Hypothesis == actual && rules.HasResult(actual, exam)) HasConfirmingExam = true;
                if (rules.Distinguishes(actual, Hypothesis, exam)) changedEvidence = true;
            }
            if (!changedEvidence || Hypothesis == actual) return false;
            Hypothesis = rules.Choose(actual, random, 0.15);
            HasConfirmingExam = Hypothesis == actual;
            return true;
        }
    }

    internal sealed class WorkerDiagnosticRules
    {
        private readonly Dictionary<string, (DiseaseDefinition Disease, double Weight)[]> alternatives;
        private readonly Dictionary<string, Dictionary<string, int[]>> signatures;
        private readonly long totalWeight;

        internal WorkerDiagnosticRules(IEnumerable<DiseaseDefinition> definitions)
        {
            var diseases = definitions.ToDictionary(disease => disease.Id, StringComparer.Ordinal);
            totalWeight = diseases.Values.Sum(disease => (long)disease.SelectionWeight);
            var tagFrequency = diseases.Values.SelectMany(disease => disease.Tags).GroupBy(tag => tag)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            alternatives = diseases.Values.ToDictionary(disease => disease.Id, disease => diseases.Values
                .Where(other => other != disease)
                .Select(other => (Disease: other, Weight: disease.Tags.Intersect(other.Tags)
                    .Sum(tag => 1d / tagFrequency[tag])))
                .Where(candidate => candidate.Weight > 0).ToArray(), StringComparer.Ordinal);

            var textIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var examIds = new ExamOrders("Workers", "Workers", "workers", null).Items.Select(exam => exam.Id).ToArray();
            signatures = new Dictionary<string, Dictionary<string, int[]>>(StringComparer.Ordinal);
            foreach (DiseaseDefinition disease in diseases.Values)
            {
                var byExam = new Dictionary<string, int[]>(StringComparer.Ordinal);
                foreach (string exam in examIds)
                {
                    var stages = new int[10];
                    for (int stage = 1; stage <= 10; stage++)
                    {
                        string phrase = disease.FindExamResult(exam, stage);
                        if (phrase == null) continue;
                        int signature;
                        if (!textIds.TryGetValue(phrase, out signature))
                        {
                            signature = textIds.Count + 1;
                            textIds.Add(phrase, signature);
                        }
                        stages[stage - 1] = signature;
                    }
                    byExam.Add(exam, stages);
                }
                signatures.Add(disease.Id, byExam);
            }
        }

        internal double Accuracy(DiseaseDefinition disease)
        {
            double frequency = (double)disease.SelectionWeight / totalWeight;
            return frequency >= 0.15 ? 0.95 : frequency >= 0.05 ? 0.85 : 0.65;
        }

        internal DiseaseDefinition Choose(DiseaseDefinition actual, Random random, double evidenceBonus)
        {
            if (random.NextDouble() < Math.Min(0.99, Accuracy(actual) + evidenceBonus)) return actual;
            var candidates = alternatives[actual.Id];
            if (candidates.Length == 0) return null;
            double roll = random.NextDouble() * candidates.Sum(candidate => candidate.Weight);
            foreach (var candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0) return candidate.Disease;
            }
            return candidates[candidates.Length - 1].Disease;
        }

        private int Signature(DiseaseDefinition disease, string exam, int stage) =>
            disease == null || stage < 1 || stage > 10 ? 0 : signatures[disease.Id][exam][stage - 1];

        internal bool HasResult(DiseaseDefinition disease, ExamOrder exam) => Signature(disease, exam.Id, exam.StageAtRequest) != 0;

        internal bool Distinguishes(DiseaseDefinition actual, DiseaseDefinition hypothesis, ExamOrder exam)
        {
            int actualResult = Signature(actual, exam.Id, exam.StageAtRequest);
            if (actualResult == 0) return false;
            if (hypothesis != null) return actualResult != Signature(hypothesis, exam.Id, exam.StageAtRequest);
            return alternatives[actual.Id].Length == 0 || alternatives[actual.Id]
                .Any(candidate => actualResult != Signature(candidate.Disease, exam.Id, exam.StageAtRequest));
        }

        internal int ExamPriority(DiseaseDefinition hypothesis, string exam, int stage)
        {
            if (hypothesis == null || Signature(hypothesis, exam, stage) == 0) return 0;
            return 1 + alternatives[hypothesis.Id].Count(candidate =>
                Signature(candidate.Disease, exam, stage) != Signature(hypothesis, exam, stage));
        }
    }
}
