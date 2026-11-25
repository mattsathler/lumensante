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
            bool numericEvidence = false;
            ExamOrder[] examList = exams.ToArray();
            foreach (ExamOrder exam in examList.SelectMany(item => item.Results))
            {
                string resultKey = exam.Id + ":" + exam.CompletedAtMilliseconds + ":" + exam.StageAtRequest + ":" + exam.SavedFindings;
                if (!reviewedExams.Add(resultKey)) continue;
                if (exam.Measurements != null && exam.Measurements.Any(item => item.Value.HasValue))
                {
                    numericEvidence = true;
                    if (Hypothesis == actual && rules.Supports(actual, exam)) HasConfirmingExam = true;
                    if (rules.Distinguishes(actual, Hypothesis, exam)) changedEvidence = true;
                    continue;
                }
                if (Hypothesis == actual && rules.HasResult(actual, exam)) HasConfirmingExam = true;
                if (rules.Distinguishes(actual, Hypothesis, exam)) changedEvidence = true;
            }
            var values = DiagnosticInvestigation.Measurements(examList);
            bool excludedByMeasurements = numericEvidence && Hypothesis != null &&
                DiagnosticInvestigation.Excluded(Hypothesis, values) && !DiagnosticInvestigation.Excluded(actual, values);
            changedEvidence |= excludedByMeasurements;
            if (!changedEvidence || Hypothesis == actual) return false;
            Hypothesis = excludedByMeasurements ? rules.ChooseCompatible(actual, random, values) : rules.Choose(actual, random, 0.15);
            HasConfirmingExam = !excludedByMeasurements && Hypothesis == actual;
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

        internal DiseaseDefinition ChooseCompatible(DiseaseDefinition actual, Random random, IReadOnlyDictionary<string, double> values)
        {
            bool actualCompatible = !DiagnosticInvestigation.Excluded(actual, values);
            if (actualCompatible && random.NextDouble() < Math.Min(0.99, Accuracy(actual) + 0.15)) return actual;
            var candidates = alternatives[actual.Id].Where(candidate => !DiagnosticInvestigation.Excluded(candidate.Disease, values)).ToArray();
            if (candidates.Length == 0) return actualCompatible ? actual : null;
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

        internal bool Supports(DiseaseDefinition disease, ExamOrder exam) => exam.Measurements != null &&
            exam.Measurements.Any(measurement => measurement.Value.HasValue &&
                (disease.ParameterRanges.ContainsKey(measurement.Id) ||
                 (measurement.Id == "leukocytes" && disease.LeukocyteRanges.Count > 0)) &&
                measurement.Value.Value >= ParameterRange(disease, measurement.Id, exam.StageAtRequest).Min &&
                measurement.Value.Value <= ParameterRange(disease, measurement.Id, exam.StageAtRequest).Max);

        internal bool Distinguishes(DiseaseDefinition actual, DiseaseDefinition hypothesis, ExamOrder exam)
        {
            if (exam.Measurements != null && exam.Measurements.Any(item => item.Value.HasValue))
            {
                foreach (var measurement in exam.Measurements.Where(item => item.Value.HasValue))
                {
                    var actualRange = ParameterRange(actual, measurement.Id, exam.StageAtRequest);
                    var hypothesisRange = hypothesis == null ? (double.MinValue, double.MaxValue) :
                        ParameterRange(hypothesis, measurement.Id, exam.StageAtRequest);
                    double value = measurement.Value.Value;
                    if (value >= actualRange.Item1 && value <= actualRange.Item2 &&
                        (value < hypothesisRange.Item1 || value > hypothesisRange.Item2)) return true;
                }
                return false;
            }
            int actualResult = Signature(actual, exam.Id, exam.StageAtRequest);
            if (actualResult == 0) return false;
            if (hypothesis != null) return actualResult != Signature(hypothesis, exam.Id, exam.StageAtRequest);
            return alternatives[actual.Id].Length == 0 || alternatives[actual.Id]
                .Any(candidate => actualResult != Signature(candidate.Disease, exam.Id, exam.StageAtRequest));
        }

        private static (double Min, double Max) ParameterRange(DiseaseDefinition disease, string parameter, int stage)
        {
            if (parameter == "leukocytes" && disease.LeukocyteRanges.Count > 0)
            {
                var range = disease.LeukocyteRanges[Math.Max(0, Math.Min(stage - 1, disease.LeukocyteRanges.Count - 1))];
                return (range.Min, range.Max);
            }
            if (disease.ParameterRanges.TryGetValue(parameter, out var ranges) && ranges.Length > 0)
                return ranges[Math.Max(0, Math.Min(stage - 1, ranges.Length - 1))];
            var reference = ClinicalParameterCatalog.Vitals.Concat(ClinicalParameterCatalog.Hematology)
                .Concat(ClinicalParameterCatalog.Chemistry).Concat(ClinicalParameterCatalog.BloodGas)
                .FirstOrDefault(item => item.Id == parameter);
            return reference == null ? (double.MinValue, double.MaxValue) : (reference.Minimum, reference.Maximum);
        }

        internal int ExamPriority(DiseaseDefinition hypothesis, string exam, int stage)
        {
            var parameters = ClinicalParameterCatalog.ForExam(exam).Select(item => item.Id).ToArray();
            if (parameters.Length > 0)
            {
                if (hypothesis == null) return 0;
                int relevant = new[] { hypothesis }.Concat(alternatives[hypothesis.Id].Select(item => item.Disease))
                    .Count(disease => disease.Exclusions.Any(rule => rule.Conditions.Any(condition => parameters.Contains(condition.Parameter))));
                relevant += parameters.Count(parameter => hypothesis.ParameterRanges.ContainsKey(parameter));
                if (parameters.Contains("leukocytes") && hypothesis.LeukocyteRanges.Count > 0) relevant++;
                return relevant;
            }
            if (hypothesis == null || Signature(hypothesis, exam, stage) == 0) return 0;
            return 1 + alternatives[hypothesis.Id].Count(candidate =>
                Signature(candidate.Disease, exam, stage) != Signature(hypothesis, exam, stage));
        }
    }
}
