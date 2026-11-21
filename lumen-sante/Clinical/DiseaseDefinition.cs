using System;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed class DiseaseDefinition
    {
        private readonly (string Prompt, string[][] AnswersByStage)[] questions;

        internal DiseaseDefinition(string id, int selectionWeight, int stageDurationMilliseconds,
            DiseaseStage[] stages, params (string Prompt, Func<int, string[]> Answers)[] questionDefinitions)
        {
            if (selectionWeight <= 0 || stageDurationMilliseconds <= 0 || stages.Length != 10)
            {
                throw new ArgumentException("Peso, duração ou estágios inválidos: " + id);
            }

            Id = id;
            SelectionWeight = selectionWeight;
            StageDurationMilliseconds = stageDurationMilliseconds;
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
        internal int SelectionWeight { get; }
        internal int StageDurationMilliseconds { get; }
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
