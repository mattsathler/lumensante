using System;
using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal static class SymptomCatalog
    {
        internal static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { "pain", "nausea", "breathlessness", "nasal-congestion" });

        internal const int InterviewIndexOffset = 200;
        internal const int HelpQuestion = InterviewIndexOffset;
        private static readonly (int Index, string Prompt, string Symptom)[] Interview =
        {
            (HelpQuestion, "Em que posso ajudar?", "opening"),
            (InterviewIndexOffset + 1, "Teve febre?", "fever"),
            (InterviewIndexOffset + 2, "Sentiu dores abdominais?", "abdominal-pain"),
            (InterviewIndexOffset + 3, "Teve náuseas ou vômitos?", "nausea"),
            (InterviewIndexOffset + 4, "Teve alterações no intestino?", "bowel-changes"),
            (InterviewIndexOffset + 5, "Sentiu falta de ar?", "breathlessness"),
            (InterviewIndexOffset + 6, "Teve tosse?", "cough"),
            (InterviewIndexOffset + 7, "Teve congestão nasal ou coriza?", "nasal-congestion"),
            (InterviewIndexOffset + 8, "Sentiu dor ou pressão no rosto?", "facial-pain"),
            (InterviewIndexOffset + 9, "Teve dor de cabeça?", "headache"),
            (InterviewIndexOffset + 10, "Sentiu dor muscular?", "muscle-pain"),
            (InterviewIndexOffset + 11, "Teve dor ou ardência ao urinar?", "urinary-symptoms"),
            (InterviewIndexOffset + 12, "Teve dor de garganta?", "sore-throat")
        };

        internal static (int Index, string Prompt)[] InterviewQuestions(DiseaseDefinition disease) =>
            Interview.Select(question => (question.Index, question.Prompt)).ToArray();

        internal static string InterviewPrompt(DiseaseDefinition disease, int questionIndex) =>
            Interview.FirstOrDefault(question => question.Index == questionIndex).Prompt;

        internal static string LegacyInterviewPrompt(int questionIndex)
        {
            switch (questionIndex)
            {
                case 100: return "Teve febre?";
                case 101: return "Sentiu dor ou desconforto?";
                case 102: return "Teve náuseas ou vontade de vomitar?";
                case 103: return "Sentiu falta de ar?";
                case 104: return "Teve congestão nasal ou coriza?";
                default: return "Sintoma";
            }
        }

        internal static string InterviewAnswer(DiseaseDefinition disease, PatientClinicalState state,
            int questionIndex, int stage, Random random)
        {
            if (questionIndex == HelpQuestion)
                return disease.PickAnswer(0, stage, random);

            string symptom = Interview.FirstOrDefault(question => question.Index == questionIndex).Symptom;
            bool present;
            switch (symptom)
            {
                case "fever":
                    present = state?.HasVitals == true && state.Vitals.Temperature.HasValue && state.Vitals.Temperature.Value >= 38;
                    return present ? "Sim, tive febre." : "Não tive febre.";
                case "abdominal-pain":
                    present = state?.PainIntensity > 0 && IsOneOf(disease?.Id,
                        "acute-gastroenteritis", "acute-intermittent-porphyria", "bacterial-cystitis");
                    break;
                case "nausea":
                    present = state?.NauseaIntensity > 0 && disease?.SymptomIds.Contains("nausea") == true;
                    break;
                case "bowel-changes":
                    present = IsOneOf(disease?.Id, "acute-gastroenteritis", "acute-intermittent-porphyria");
                    break;
                case "breathlessness":
                    present = state?.HasVitals == true && state.BreathlessnessIntensity > 0 &&
                        disease?.SymptomIds.Contains("breathlessness") == true;
                    break;
                case "cough":
                    present = IsOneOf(disease?.Id, "flu-like", "viral-pharyngitis");
                    break;
                case "nasal-congestion":
                    present = state?.NasalCongestionIntensity > 0 && disease?.SymptomIds.Contains("nasal-congestion") == true;
                    break;
                case "facial-pain":
                    present = state?.PainIntensity > 0 && IsOneOf(disease?.Id, "acute-sinusitis", "allergic-rhinitis");
                    break;
                case "headache":
                    present = state?.PainIntensity > 0 && IsOneOf(disease?.Id,
                        "tension-headache", "acute-sinusitis", "flu-like", "allergic-rhinitis");
                    break;
                case "muscle-pain":
                    present = state?.PainIntensity > 0 && IsOneOf(disease?.Id, "muscle-strain", "flu-like");
                    break;
                case "urinary-symptoms":
                    present = disease?.Id == "bacterial-cystitis";
                    break;
                case "sore-throat":
                    present = disease?.Id == "viral-pharyngitis";
                    break;
                default:
                    return "Não.";
            }
            return present ? "Sim." : "Não.";
        }

        internal static string BriefDescription(DiseaseDefinition disease)
        {
            var symptoms = new List<string>();
            if (disease.Stages.Take(disease.Stages.Count - 1).Any(stage => stage.TemperatureTenths.Max >= 380))
                symptoms.Add("febre");
            foreach (var question in Interview.Skip(1))
            {
                if (!CanOccur(disease, question.Symptom)) continue;
                string description = SymptomDescription(question.Symptom);
                if (!string.IsNullOrWhiteSpace(description)) symptoms.Add(description);
            }
            symptoms = symptoms.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct().ToList();
            return symptoms.Count == 0
                ? "Pode se manifestar com sinais e sintomas variados."
                : "Pode apresentar " + JoinPortuguese(symptoms) + ".";
        }

        private static bool CanOccur(DiseaseDefinition disease, string symptom)
        {
            switch (symptom)
            {
                case "abdominal-pain": return disease.SymptomIds.Contains("pain") &&
                    IsOneOf(disease.Id, "acute-gastroenteritis", "acute-intermittent-porphyria", "bacterial-cystitis");
                case "nausea": return disease.SymptomIds.Contains("nausea");
                case "bowel-changes": return IsOneOf(disease.Id, "acute-gastroenteritis", "acute-intermittent-porphyria");
                case "breathlessness": return disease.SymptomIds.Contains("breathlessness");
                case "cough": return IsOneOf(disease.Id, "flu-like", "viral-pharyngitis");
                case "nasal-congestion": return disease.SymptomIds.Contains("nasal-congestion");
                case "facial-pain": return disease.SymptomIds.Contains("pain") && IsOneOf(disease.Id, "acute-sinusitis", "allergic-rhinitis");
                case "headache": return disease.SymptomIds.Contains("pain") &&
                    IsOneOf(disease.Id, "tension-headache", "acute-sinusitis", "flu-like", "allergic-rhinitis");
                case "muscle-pain": return disease.SymptomIds.Contains("pain") && IsOneOf(disease.Id, "muscle-strain", "flu-like");
                case "urinary-symptoms": return disease.Id == "bacterial-cystitis";
                case "sore-throat": return disease.Id == "viral-pharyngitis";
                default: return false;
            }
        }

        private static bool IsOneOf(string value, params string[] choices) => choices.Contains(value, StringComparer.Ordinal);

        private static string JoinPortuguese(IReadOnlyList<string> values) => values.Count == 1 ? values[0] :
            string.Join(", ", values.Take(values.Count - 1)) + " e " + values[values.Count - 1];

        private static string SymptomDescription(string symptom)
        {
            switch (symptom)
            {
                case "abdominal-pain": return "dor abdominal";
                case "nausea": return "náusea ou vômitos";
                case "bowel-changes": return "alterações intestinais";
                case "breathlessness": return "falta de ar";
                case "cough": return "tosse";
                case "nasal-congestion": return "congestão nasal ou coriza";
                case "facial-pain": return "dor ou pressão no rosto";
                case "headache": return "dor de cabeça";
                case "muscle-pain": return "dor muscular";
                case "urinary-symptoms": return "dor ou ardência ao urinar";
                case "sore-throat": return "dor de garganta";
                default: return null;
            }
        }
    }
}
