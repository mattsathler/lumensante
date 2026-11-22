using System;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed class DiseaseDefinition
    {
        private readonly (string Prompt, string[][] AnswersByStage)[] questions;

        internal DiseaseDefinition(string id, int selectionWeight, int minimumStageDurationMilliseconds,
            int maximumStageDurationMilliseconds,
            DiseaseStage[] stages, double worseningRisk, double treatmentSuccessRate,
            string name, string treatmentName,
            params (string Prompt, Func<int, string[]> Answers)[] questionDefinitions)
        {
            if (selectionWeight <= 0 || minimumStageDurationMilliseconds <= 0 ||
                maximumStageDurationMilliseconds < minimumStageDurationMilliseconds || stages.Length != 10)
            {
                throw new ArgumentException("Peso, duração ou estágios inválidos: " + id);
            }

            Id = id;
            Name = name;
            TreatmentName = treatmentName;
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
        internal string Name { get; }
        internal string TreatmentName { get; }
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
