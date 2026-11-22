using System;
using System.Linq;
using System.Collections.Generic;

namespace lumen_sante.Clinical
{
    internal sealed class DiseaseDefinition
    {
        private readonly (string Prompt, string[][] AnswersByStage)[] questions;
        private readonly Dictionary<string, string[]> examFindings;

        internal DiseaseDefinition(string id, int selectionWeight, int minimumStageDurationMilliseconds,
            int maximumStageDurationMilliseconds,
            DiseaseStage[] stages, double worseningRisk, double treatmentSuccessRate,
            string name, string treatmentName, bool requiresSpecificTreatment, MedicationEffect[] medicationEffects,
            string[] tags, Dictionary<string, string[]> exams,
            params (string Prompt, Func<int, string[]> Answers)[] questionDefinitions)
        {
            if (selectionWeight <= 0 || minimumStageDurationMilliseconds <= 0 ||
                maximumStageDurationMilliseconds < minimumStageDurationMilliseconds || stages.Length != 10)
            {
                throw new ArgumentException("Peso, duração ou estágios inválidos: " + id);
            }

            Id = id;
            Tags = Array.AsReadOnly((string[])tags.Clone());
            examFindings = exams.ToDictionary(pair => pair.Key, pair => (string[])pair.Value.Clone(), StringComparer.Ordinal);
            Name = name;
            TreatmentName = treatmentName;
            RequiresSpecificTreatment = requiresSpecificTreatment;
            if (requiresSpecificTreatment && string.IsNullOrWhiteSpace(treatmentName))
                throw new ArgumentException("Tratamento específico obrigatório: " + id);
            if (medicationEffects == null || medicationEffects.Any(effect => effect == null) ||
                medicationEffects.Select(effect => effect.Medication).Distinct().Count() != medicationEffects.Length)
                throw new ArgumentException("Efeitos de medicamentos inválidos: " + id);
            MedicationEffects = Array.AsReadOnly((MedicationEffect[])medicationEffects.Clone());
            if (double.IsNaN(worseningRisk) || worseningRisk < 0 || worseningRisk > 1 ||
                double.IsNaN(treatmentSuccessRate) || treatmentSuccessRate < 0 || treatmentSuccessRate > 1)
            {
                throw new ArgumentException("Probabilidades inválidas: " + id);
            }
            WorseningRisk = worseningRisk;
            TreatmentSuccessRate = treatmentSuccessRate;
            SelectionWeight = selectionWeight;
            MinimumStageDurationMilliseconds = minimumStageDurationMilliseconds;
            MaximumStageDurationMilliseconds = maximumStageDurationMilliseconds;
            Stages = Array.AsReadOnly(stages);
            questions = new (string, string[][])[questionDefinitions.Length];

            for (int question = 0; question < questions.Length; question++)
            {
                var definition = questionDefinitions[question];
                var answers = new string[TerminalIndex][];
                for (int stage = 0; stage < answers.Length; stage++)
                {
                    answers[stage] = definition.Answers(stage);
                    if (answers[stage].Any(string.IsNullOrWhiteSpace) ||
                        answers[stage].Distinct(StringComparer.OrdinalIgnoreCase).Count() < 20)
                    {
                        throw new ArgumentException($"O caso {id} precisa de 20 respostas distintas no estágio {stage + 1}: {definition.Prompt}");
                    }
                }

                questions[question] = (definition.Prompt, answers);
            }

            Prompts = Array.AsReadOnly(questions.Select(question => question.Prompt).ToArray());
        }

        internal string Id { get; }
        internal IReadOnlyList<string> Tags { get; }
        internal string FindExamResult(string examId, int stage)
        {
            string[] findings;
            return stage >= 1 && stage <= Stages.Count && examFindings.TryGetValue(examId, out findings)
                ? findings[stage - 1] : null;
        }
        internal string Name { get; }
        internal string TreatmentName { get; }
        internal bool RequiresSpecificTreatment { get; }
        internal System.Collections.Generic.IReadOnlyList<MedicationEffect> MedicationEffects { get; }
        internal string TreatmentLabel => TreatmentName + " para " + Name;

        internal (double Improvement, double Worsening) Chances(int severityIndex, bool correctTreatment)
        {
            double severity = Math.Min(1d, Math.Max(0d, (double)severityIndex / (TerminalIndex - 1)));
            if (!correctTreatment) return (0d, WorseningRisk);
            double improvement = TreatmentSuccessRate * (2d - 1.65d * severity);
            double worsening = WorseningRisk * (0.15d + 1.15d * severity);
            double total = Math.Max(1d, improvement + worsening);
            return (improvement / total, worsening / total);
        }

        internal int Evolve(int riskLevel, bool correctTreatment,
            System.Collections.Generic.IEnumerable<MedicationClass> medications, Random random)
        {
            if (riskLevel < 0 || riskLevel > Stages.Count) throw new ArgumentOutOfRangeException(nameof(riskLevel));
            if (riskLevel == 0 || riskLevel == Stages.Count) return riskLevel;
            bool effectiveTreatment = RequiresSpecificTreatment && correctTreatment;
            var effects = MedicationEffects.Where(effect => medications.Contains(effect.Medication)).ToArray();
            var chances = Chances(riskLevel - 1, effectiveTreatment);
            double severity = (double)(riskLevel - 1) / (TerminalIndex - 1);
            double improvement = Math.Max(chances.Improvement,
                effects.Select(effect => effect.ImprovementChance * (1d - 0.7d * severity)).DefaultIfEmpty(0d).Max());
            double worsening = chances.Worsening * effects.Select(effect => effect.WorseningMultiplier).DefaultIfEmpty(1d).Min();
            double total = Math.Max(1d, improvement + worsening);
            double roll = random.NextDouble();
            bool canRecover = RequiresSpecificTreatment ? effectiveTreatment : effects.Length > 0;
            if (roll < improvement / total) return Math.Max(canRecover ? 0 : 1, riskLevel - 1);
            if (roll < (improvement + worsening) / total) return Math.Min(Stages.Count, riskLevel + 1);
            return riskLevel;
        }
        internal double WorseningRisk { get; }
        internal double TreatmentSuccessRate { get; }
        internal int SelectionWeight { get; }
        internal int MinimumStageDurationMilliseconds { get; }
        internal int MaximumStageDurationMilliseconds { get; }
        internal System.Collections.Generic.IReadOnlyList<DiseaseStage> Stages { get; }
        internal System.Collections.Generic.IReadOnlyList<string> Prompts { get; }
        internal int TerminalIndex => Stages.Count - 1;

        internal string PickAnswer(int question, int stage, Random random)
        {
            string[] answers = questions[question].AnswersByStage[stage];
            return answers[random.Next(answers.Length)];
        }
    }
}
