using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace lumen_sante.Clinical
{
    internal static class DiseaseJsonReader
    {
        private static readonly HashSet<string> ExamIds = new HashSet<string>(
            new ExamOrders("Catalog", "Catalog", "catalog", null).Items.Select(exam => exam.Id), StringComparer.Ordinal);

        internal static DiseaseDefinition Read(string json)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024, RecursionLimit = 64 };
            var root = Object(serializer.DeserializeObject(json), "$", "SchemaVersion", "Id", "Name", "SelectionWeight",
                "Tags", "MinimumStageDurationMilliseconds", "MaximumStageDurationMilliseconds", "WorseningRisk",
                "TreatmentSuccessRate", "RequiresSpecificTreatment", "TreatmentName", "MedicationEffects", "Stages", "Questions", "Exams");
            Integer(root, "SchemaVersion", "$", 1, 1);
            string id = Text(root, "Id", "$");
            if (!Regex.IsMatch(id, "^[a-z0-9]+(?:-[a-z0-9]+)*$")) throw Error("$.Id", "use letras minúsculas, números e hífens");
            string name = Text(root, "Name", "$");
            int weight = Integer(root, "SelectionWeight", "$", 1, int.MaxValue);
            int minimum = Integer(root, "MinimumStageDurationMilliseconds", "$", 1, int.MaxValue - 1);
            int maximum = Integer(root, "MaximumStageDurationMilliseconds", "$", minimum, int.MaxValue - 1);
            double worsening = Probability(root, "WorseningRisk", "$");
            double success = Probability(root, "TreatmentSuccessRate", "$");
            object requiresValue = Required(root, "RequiresSpecificTreatment", "$");
            if (!(requiresValue is bool)) throw Error("$.RequiresSpecificTreatment", "esperado booleano");
            bool requires = (bool)requiresValue;
            object treatmentValue = Required(root, "TreatmentName", "$");
            string treatment = treatmentValue == null ? null : NonEmptyText(treatmentValue, "$.TreatmentName");
            if (requires && treatment == null) throw Error("$.TreatmentName", "obrigatório para tratamento específico");
            var tags = Array(root, "Tags", "$").Select((value, index) => NonEmptyText(value, "$.Tags[" + index + "]")).ToArray();
            if (tags.Length == 0 || tags.Distinct(StringComparer.Ordinal).Count() != tags.Length ||
                tags.Any(tag => !Regex.IsMatch(tag, "^[a-z0-9]+(?:-[a-z0-9]+)*$")))
                throw Error("$.Tags", "informe tags únicas em letras minúsculas, números e hífens");

            var effects = Array(root, "MedicationEffects", "$").Select((value, index) => ReadEffect(value, "$.MedicationEffects[" + index + "]")).ToArray();
            if (effects.Select(effect => effect.Medication).Distinct().Count() != effects.Length)
                throw Error("$.MedicationEffects", "classe duplicada");
            var stageValues = Array(root, "Stages", "$");
            if (stageValues.Length != 10) throw Error("$.Stages", "esperados dez estágios");
            var stages = stageValues.Select((value, index) => ReadStage(value, index)).ToArray();
            var questionValues = Array(root, "Questions", "$");
            if (questionValues.Length == 0) throw Error("$.Questions", "esperada pergunta inicial");
            var questions = questionValues.Select((value, index) => ReadQuestion(value, index)).ToArray();
            if (questions[0].Prompt != "Como você está se sentindo?")
                throw Error("$.Questions[0].Prompt", "a pergunta inicial deve ser Como você está se sentindo?");
            if (questions.Select(question => question.Prompt).Distinct(StringComparer.OrdinalIgnoreCase).Count() != questions.Length)
                throw Error("$.Questions", "perguntas duplicadas");
            var exams = ReadExams(Required(root, "Exams", "$"));
            return new DiseaseDefinition(id, weight, minimum, maximum, stages, worsening, success,
                name, treatment, requires, effects, tags, exams, questions);
        }

        private static MedicationEffect ReadEffect(object value, string path)
        {
            var data = Object(value, path, "Medication", "ImprovementChance", "WorseningMultiplier");
            string name = Text(data, "Medication", path);
            if (!Enum.GetNames(typeof(MedicationClass)).Contains(name))
                throw Error(path + ".Medication", "classe desconhecida " + name);
            return new MedicationEffect((MedicationClass)Enum.Parse(typeof(MedicationClass), name),
                Probability(data, "ImprovementChance", path), Probability(data, "WorseningMultiplier", path));
        }

        private static DiseaseStage ReadStage(object value, int index)
        {
            string path = "$.Stages[" + index + "]";
            var data = Object(value, path, "Pulse", "Systolic", "Diastolic", "RespiratoryRate", "OxygenSaturation", "TemperatureTenths", "Inspection");
            bool terminal = index == 9;
            var pulse = Range(data, "Pulse", path, terminal, 1, int.MaxValue - 1);
            var systolic = Range(data, "Systolic", path, terminal, 1, int.MaxValue - 1);
            var diastolic = Range(data, "Diastolic", path, terminal, 1, int.MaxValue - 1);
            var breathing = Range(data, "RespiratoryRate", path, terminal, 1, int.MaxValue - 1);
            var oxygen = Range(data, "OxygenSaturation", path, terminal, 1, 100);
            var temperature = Range(data, "TemperatureTenths", path, terminal, 300, 430);
            var inspection = Array(data, "Inspection", path);
            if (inspection.Length != 3) throw Error(path + ".Inspection", "esperados estado geral, respiração e pele");
            return new DiseaseStage(pulse, systolic, diastolic, breathing, oxygen,
                (NonEmptyText(inspection[0], path + ".Inspection[0]"), NonEmptyText(inspection[1], path + ".Inspection[1]"),
                 NonEmptyText(inspection[2], path + ".Inspection[2]")), temperature);
        }

        private static (int Min, int Max) Range(Dictionary<string, object> data, string key, string path, bool terminal, int minimum, int maximum)
        {
            object[] range = Array(data, key, path);
            if (range.Length != 2) throw Error(path + "." + key, "esperada faixa [mínimo, máximo]");
            int min = NumberInteger(range[0], path + "." + key + "[0]", terminal ? 0 : minimum, terminal ? 0 : maximum);
            int max = NumberInteger(range[1], path + "." + key + "[1]", min, terminal ? 0 : maximum);
            return (min, max);
        }

        private static (string Prompt, Func<int, string[]> Answers) ReadQuestion(object value, int index)
        {
            string path = "$.Questions[" + index + "]";
            var data = Object(value, path, "Prompt", "AnswersByStage");
            string prompt = Text(data, "Prompt", path);
            var stages = Array(data, "AnswersByStage", path);
            if (stages.Length != 9) throw Error(path + ".AnswersByStage", "esperados nove bancos de respostas");
            var answers = stages.Select((stage, stageIndex) =>
            {
                string answerPath = path + ".AnswersByStage[" + stageIndex + "]";
                var items = stage as object[];
                if (items == null) throw Error(answerPath, "esperada lista de frases");
                var phrases = items.Select((item, phraseIndex) => NonEmptyText(item, answerPath + "[" + phraseIndex + "]")).ToArray();
                if (phrases.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 20)
                    throw Error(answerPath, "esperadas pelo menos vinte frases distintas");
                return phrases;
            }).ToArray();
            return (prompt, stage => answers[stage]);
        }

        private static Dictionary<string, string[]> ReadExams(object value)
        {
            var data = Object(value, "$.Exams");
            var results = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var pair in data)
            {
                string path = "$.Exams." + pair.Key;
                if (!ExamIds.Contains(pair.Key)) throw Error(path, "ID de exame desconhecido");
                var phrases = new string[10];
                if (pair.Value != null)
                {
                    var exam = Object(pair.Value, path, "Default", "ByStage");
                    object defaultValue = Required(exam, "Default", path);
                    string defaultText = defaultValue == null ? null : NonEmptyText(defaultValue, path + ".Default");
                    for (int index = 0; index < phrases.Length; index++) phrases[index] = defaultText;
                    var overrides = Object(Required(exam, "ByStage", path), path + ".ByStage");
                    foreach (var stage in overrides)
                    {
                        int number;
                        if (!int.TryParse(stage.Key, NumberStyles.None, CultureInfo.InvariantCulture, out number) ||
                            number < 1 || number > 10 || number.ToString(CultureInfo.InvariantCulture) != stage.Key)
                            throw Error(path + ".ByStage." + stage.Key, "estágio deve estar entre 1 e 10");
                        phrases[number - 1] = stage.Value == null ? null : NonEmptyText(stage.Value, path + ".ByStage." + stage.Key);
                    }
                }
                results.Add(pair.Key, phrases);
            }
            return results;
        }

        private static Dictionary<string, object> Object(object value, string path, params string[] allowed)
        {
            var data = value as Dictionary<string, object>;
            if (data == null) throw Error(path, "esperado objeto JSON");
            if (allowed.Length > 0)
            {
                foreach (string key in data.Keys)
                    if (!allowed.Contains(key)) throw Error(path + "." + key, "campo desconhecido");
            }
            return data;
        }

        private static object Required(Dictionary<string, object> data, string key, string path)
        {
            object value;
            if (!data.TryGetValue(key, out value)) throw Error(path + "." + key, "campo obrigatório ausente");
            return value;
        }

        private static object[] Array(Dictionary<string, object> data, string key, string path)
        {
            var values = Required(data, key, path) as object[];
            if (values == null) throw Error(path + "." + key, "esperada lista");
            return values;
        }

        private static string NonEmptyText(object value, string path)
        {
            string text = value as string;
            if (string.IsNullOrWhiteSpace(text)) throw Error(path, "esperada frase não vazia");
            return text;
        }

        private static string Text(Dictionary<string, object> data, string key, string path) => NonEmptyText(Required(data, key, path), path + "." + key);

        private static int Integer(Dictionary<string, object> data, string key, string path, int min, int max) =>
            NumberInteger(Required(data, key, path), path + "." + key, min, max);

        private static int NumberInteger(object value, string path, int min, int max)
        {
            if (!(value is int) && !(value is long)) throw Error(path, "esperado número inteiro");
            long number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (number < min || number > max) throw Error(path, "número fora da faixa " + min + " a " + max);
            return (int)number;
        }

        private static double Probability(Dictionary<string, object> data, string key, string path)
        {
            object value = Required(data, key, path);
            if (!(value is int) && !(value is long) && !(value is decimal) && !(value is double))
                throw Error(path + "." + key, "esperado número entre 0 e 1");
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || double.IsInfinity(number) || number < 0 || number > 1)
                throw Error(path + "." + key, "esperado número entre 0 e 1");
            return number;
        }

        private static ArgumentException Error(string path, string reason) => new ArgumentException(path + ": " + reason);
    }
}
