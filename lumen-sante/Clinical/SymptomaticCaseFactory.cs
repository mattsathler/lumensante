using System;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal enum SymptomaticProfile { Local, Infection, Digestive, Respiratory, Skin }
    internal enum TemperatureProfile { Normal, MildFever, ComplicatedInfection }

    // Shared simulation curves; late stages model generic complications, not natural disease timelines.
    internal static class SymptomaticCaseFactory
    {
        private static readonly string[][] Details =
        {
            new[] { "Ainda consigo fazer minhas tarefas, mas preciso de pausas.", "O incômodo atrapalhou meu sono nesta noite.", "Tenho me concentrado menos no trabalho por causa disso.", "Parei de fazer exercício desde que isso começou.", "Tenho preferido descansar quando chego em casa." },
            new[] { "Já precisei faltar ao trabalho por causa disso.", "Estou dependendo de ajuda para algumas tarefas.", "O incômodo me acorda várias vezes durante a noite.", "Estou fazendo menos refeições desde que piorei.", "Preciso parar até quando faço tarefas leves." },
            new[] { "Estou fraco demais para cuidar de mim sem ajuda.", "Tenho dificuldade para ficar em pé por muito tempo.", "Estou conseguindo beber muito pouco hoje.", "Não consegui descansar mesmo ficando deitado.", "Preciso que alguém fique comigo porque estou muito debilitado." }
        };

        internal static DiseaseDefinition Create(string id, string name, double risk, int minHours, int maxHours,
            SymptomaticProfile profile, string[] inspection, MedicationEffect[] medications, string[][] symptoms,
            (string Prompt, string[] Answers) associated, (string Prompt, string[] Answers) triggers,
            TemperatureProfile temperatureProfile = TemperatureProfile.Normal)
        {
            var stages = Enumerable.Range(0, 9).Select(stage => Stage(stage, profile, inspection, temperatureProfile)).Concat(new[]
            {
                new DiseaseStage((0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
                    ("Sem resposta", "Sem movimento respiratório", "—"))
            }).ToArray();
            return new DiseaseDefinition(id, 8, minHours * 60 * 60000, maxHours * 60 * 60000,
                stages, risk, 0, name, null, false, medications,
                ("Como você está se sentindo?", stage => Responses(symptoms[stage / 3], stage)),
                ("Os sintomas começaram há quanto tempo?", stage => Responses(new[]
                {
                    "Percebi esse incômodo nos últimos dias.", "Comecei a notar isso nesta semana.",
                    "Faz poucos dias que comecei a me sentir diferente.", "É um problema recente, apareceu nos últimos dias."
                }, stage)),
                (associated.Prompt, stage => Responses(associated.Answers, stage)),
                (triggers.Prompt, stage => Responses(triggers.Answers, stage)),
                ("Como os sintomas afetam sua rotina?", stage => Responses(stage < 3 ? new[]
                {
                    "Estou mais cansado do que de costume.", "Preciso descansar mais entre minhas atividades.",
                    "O desconforto dificulta minha concentração.", "Tenho evitado sair por causa desse incômodo."
                } : stage < 6 ? new[]
                {
                    "Já não consigo manter meu ritmo habitual.", "Tenho dificuldade para terminar tarefas simples.",
                    "O desconforto está tomando boa parte do meu dia.", "Preciso me deitar várias vezes ao longo do dia."
                } : new[]
                {
                    "Não estou conseguindo manter minha rotina.", "Preciso de ajuda até nas atividades básicas.",
                    "Estou passando o dia deitado e sem forças.", "Até vir ao hospital foi muito difícil."
                }, stage)));
        }

        private static string[] Responses(string[] statements, int stage) => statements
            .SelectMany(statement => Details[stage / 3].Select(detail => statement + " " + detail)).ToArray();

        private static DiseaseStage Stage(int stage, SymptomaticProfile profile, string[] inspection,
            TemperatureProfile temperatureProfile)
        {
            int[] pulse = { 72, 76, 80, 86, 92, 100, 110, 120, 132 };
            int[] systolic = { 112, 110, 108, 106, 102, 96, 88, 78, 68 };
            int[] diastolic = { 70, 68, 66, 64, 62, 58, 52, 46, 40 };
            int[] breaths = { 14, 14, 16, 18, 20, 22, 26, 29, 32 };
            int[] oxygen = profile == SymptomaticProfile.Respiratory
                ? new[] { 95, 94, 93, 91, 89, 86, 82, 76, 68 }
                : new[] { 97, 97, 96, 96, 95, 93, 90, 85, 78 };
            int pulseOffset = profile == SymptomaticProfile.Infection ? 6 : 0;
            string breathing = profile == SymptomaticProfile.Respiratory
                ? (stage < 3 ? "Chiado expiratório discreto" : stage < 6 ? "Chiado e esforço respiratório" : "Esforço respiratório intenso")
                : (stage < 3 ? "Sem esforço respiratório aparente" : stage < 6 ? "Respiração acelerada" : "Respiração trabalhosa");
            string skin = profile == SymptomaticProfile.Skin
                ? (stage < 3 ? "Áreas avermelhadas e sinais de coçadura" : stage < 6 ? "Lesões cutâneas inflamadas e escoriações" : "Lesões extensas e palidez")
                : (stage < 3 ? "Sem alteração visível" : stage < 6 ? "Palidez discreta" : "Palidez e extremidades frias");
            return new DiseaseStage((pulse[stage] + pulseOffset, pulse[stage] + pulseOffset + 12),
                (systolic[stage], systolic[stage] + 14), (diastolic[stage], diastolic[stage] + 10),
                (breaths[stage], breaths[stage] + 4), (oxygen[stage], Math.Min(99, oxygen[stage] + 3)),
                (inspection[stage / 3], breathing, skin), TemperatureRange(stage, temperatureProfile));
        }

        private static (int Min, int Max) TemperatureRange(int stage, TemperatureProfile profile)
        {
            // Simulation ranges in tenths of a degree Celsius; fever is specific to the case.
            if (profile == TemperatureProfile.MildFever)
                return stage < 3 ? (365, 381) : stage < 6 ? (375, 389) : (382, 398);
            if (profile == TemperatureProfile.ComplicatedInfection)
                return stage < 3 ? (360, 374) : stage < 6 ? (370, 382) : (383, 400);
            return (360, 374);
        }
    }
}
