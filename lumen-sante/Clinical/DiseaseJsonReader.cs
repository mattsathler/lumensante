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
                "Tags", "Symptoms", "Complexity", "NaturalRecoveryIntervals", "DiagnosticExclusions", "LeukocyteRanges", "ParameterRanges", "MinimumStageDurationMilliseconds", "MaximumStageDurationMilliseconds", "WorseningRisk",
                "TreatmentSuccessRate", "RequiresSpecificTreatment", "TreatmentName", "MedicationEffects", "Stages", "Questions", "Exams");
            Integer(root, "SchemaVersion", "$", 1, 1);
            string id = Text(root, "Id", "$");
            string complexity = root.ContainsKey("Complexity") ? Text(root, "Complexity", "$") : "investigative";
            if (complexity != "simple" && complexity != "investigative" && complexity != "complex")
                throw Error("$.Complexity", "use simple, investigative ou complex");
            int naturalRecoveryIntervals = root.ContainsKey("NaturalRecoveryIntervals")
                ? Integer(root, "NaturalRecoveryIntervals", "$", 1, 1000) : 0;
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

            string[] symptomIds;
            if (root.ContainsKey("Symptoms"))
            {
                symptomIds = Array(root, "Symptoms", "$").Select((value, index) => NonEmptyText(value, "$.Symptoms[" + index + "]")).ToArray();
                if (symptomIds.Distinct(StringComparer.Ordinal).Count() != symptomIds.Length || symptomIds.Any(idValue => !SymptomCatalog.Ids.Contains(idValue)))
                    throw Error("$.Symptoms", "sintoma desconhecido ou duplicado");
            }
            else
            {
                var inferred = new List<string>();
                if (tags.Any(tag => tag == "dor" || tag == "dor-abdominal" || tag == "urinaria" || tag == "digestiva")) inferred.Add("pain");
                if (tags.Any(tag => tag == "digestiva" || tag == "metabolica")) inferred.Add("nausea");
                if (tags.Contains("dispneia")) inferred.Add("breathlessness");
                symptomIds = inferred.ToArray();
            }

            var effects = Array(root, "MedicationEffects", "$").Select((value, index) => ReadEffect(value, "$.MedicationEffects[" + index + "]")).ToArray();
            if (effects.Select(effect => effect.Medication).Distinct().Count() != effects.Length)
                throw Error("$.MedicationEffects", "classe duplicada");
            var stageValues = Array(root, "Stages", "$");
            if (stageValues.Length != 10) throw Error("$.Stages", "esperados dez estágios");
            var stages = stageValues.Select((value, index) => ReadStage(value, index)).ToArray();
            var leukocytes = root.ContainsKey("LeukocyteRanges") ? ReadLeukocyteRanges(Array(root, "LeukocyteRanges", "$")) : new (int Min, int Max)[0];
            var parameterRanges = root.ContainsKey("ParameterRanges") ? ReadParameterRanges(Required(root, "ParameterRanges", "$")) :
                new Dictionary<string, (double Min, double Max)[]>(StringComparer.Ordinal);
            var questionValues = Array(root, "Questions", "$");
            if (questionValues.Length == 0) throw Error("$.Questions", "esperada pergunta inicial");
            var questions = questionValues.Select((value, index) => ReadQuestion(value, index)).ToArray();
            if (questions[0].Prompt != "Como você está se sentindo?")
                throw Error("$.Questions[0].Prompt", "a pergunta inicial deve ser Como você está se sentindo?");
            if (questions.Select(question => question.Prompt).Distinct(StringComparer.OrdinalIgnoreCase).Count() != questions.Length)
                throw Error("$.Questions", "perguntas duplicadas");
            var exams = root.ContainsKey("Exams") ? ReadExams(Required(root, "Exams", "$")) :
                new Dictionary<string, string[]>(StringComparer.Ordinal);
            var exclusions = root.ContainsKey("DiagnosticExclusions") ? ReadExclusions(Array(root, "DiagnosticExclusions", "$")) : new DiagnosticExclusionRule[0];
            ValidateExclusions(exclusions, stages, leukocytes, parameterRanges);
            return new DiseaseDefinition(id, weight, minimum, maximum, stages, worsening, success,
                name, treatment, requires, effects, tags, symptomIds, exams, complexity, naturalRecoveryIntervals, exclusions, leukocytes, parameterRanges, questions);
        }

        private static Dictionary<string, (double Min, double Max)[]> ReadParameterRanges(object value)
        {
            var parameters = Object(value, "$.ParameterRanges");
            var result = new Dictionary<string, (double Min, double Max)[]>(StringComparer.Ordinal);
            foreach (var pair in parameters)
            {
                string path = "$.ParameterRanges." + pair.Key;
                if (!ClinicalParameterCatalog.Chemistry.Concat(ClinicalParameterCatalog.Hematology.Where(item => item.Id != "leukocytes")).Any(item => item.Id == pair.Key)) throw Error(path, "parâmetro desconhecido");
                var ranges = pair.Value as object[];
                if (ranges == null || ranges.Length != 9) throw Error(path, "esperadas nove faixas");
                result.Add(pair.Key, ranges.Select((rangeValue, index) =>
                {
                    var range = rangeValue as object[];
                    if (range == null || range.Length != 2) throw Error(path, "faixa inválida no estágio " + (index + 1));
                    double min = Numeric(range[0], path); double max = Numeric(range[1], path);
                    if (min < 0 || max < min || max > 1000000) throw Error(path, "faixa fora dos limites");
                    return (Min: min, Max: max);
                }).ToArray());
            }
            return result;
        }

        private static (int Min, int Max)[] ReadLeukocyteRanges(object[] ranges)
        {
            if (ranges.Length != 9) throw Error("$.LeukocyteRanges", "esperadas nove faixas");
            return ranges.Select((value, index) =>
            {
                string path = "$.LeukocyteRanges[" + index + "]";
                var range = value as object[];
                if (range == null || range.Length != 2) throw Error(path, "esperada faixa [mínimo, máximo]");
                int min = NumberInteger(range[0], path + "[0]", 1, 1000000);
                int max = NumberInteger(range[1], path + "[1]", min, 1000000);
                return (Min: min, Max: max);
            }).ToArray();
        }

        private static void ValidateExclusions(DiagnosticExclusionRule[] rules, DiseaseStage[] stages, (int Min, int Max)[] leukocytes,
            Dictionary<string, (double Min, double Max)[]> parameters)
        {
            var living = stages.Take(stages.Length - 1).ToArray();
            foreach (var rule in rules)
            {
                bool overlapsPossibleValues = rule.Conditions.All(condition =>
                {
                    if (condition.Parameter == "leukocytes")
                    {
                        if (leukocytes.Length == 0) return true;
                        double labMin = Math.Min(4000, leukocytes.Min(range => range.Min));
                        double labMax = Math.Max(11000, leukocytes.Max(range => range.Max));
                        return condition.Minimum <= labMax && condition.Maximum >= labMin;
                    }
                    if (condition.Parameter == "arterial-pao2")
                    {
                        double oxygenMin = Math.Min(95, living.Min(stage => stage.OxygenSaturation.Min));
                        double oxygenMax = Math.Max(100, living.Max(stage => stage.OxygenSaturation.Max));
                        return condition.Minimum <= 4 * oxygenMax - 300 && condition.Maximum >= 4 * oxygenMin - 300;
                    }
                    var chemistry = ClinicalParameterCatalog.Chemistry.Concat(ClinicalParameterCatalog.Hematology.Where(item => item.Id != "leukocytes")).FirstOrDefault(item => item.Id == condition.Parameter);
                    if (chemistry != null)
                    {
                        if (!parameters.TryGetValue(condition.Parameter, out var ranges)) return condition.Minimum <= chemistry.Maximum && condition.Maximum >= chemistry.Minimum;
                        double chemistryMin = Math.Min(chemistry.Minimum, ranges.Min(range => range.Min));
                        double chemistryMax = Math.Max(chemistry.Maximum, ranges.Max(range => range.Max));
                        return condition.Minimum <= chemistryMax && condition.Maximum >= chemistryMin;
                    }
                    (int Min, int Max) Range(DiseaseStage stage)
                    {
                        switch (condition.Parameter)
                        {
                            case "pulse": return stage.Pulse;
                            case "systolic-pressure": return stage.Systolic;
                            case "diastolic-pressure": return stage.Diastolic;
                            case "respiratory-rate": return stage.RespiratoryRate;
                            case "oxygen-saturation": return stage.OxygenSaturation;
                            default: return stage.TemperatureTenths;
                        }
                    }
                    double scale = condition.Parameter == "temperature" ? 10d : 1d;
                    double min = living.Min(stage => Range(stage).Min) / scale;
                    double max = living.Max(stage => Range(stage).Max) / scale;
                    if (condition.Parameter == "temperature") { min = Math.Min(min, 36); max = Math.Max(max, 37.4); }
                    if (condition.Parameter == "oxygen-saturation") max = Math.Max(max, 95);
                    return condition.Minimum <= max && condition.Maximum >= min;
                });
                if (overlapsPossibleValues)
                    throw Error("$.DiagnosticExclusions", "regra pode excluir resultados gerados pela própria doença");
            }
        }

        private static DiagnosticExclusionRule[] ReadExclusions(object[] rules)
        {
            var ids = new HashSet<string>(ClinicalParameterCatalog.Vitals.Concat(ClinicalParameterCatalog.Hematology)
                .Concat(ClinicalParameterCatalog.Chemistry).Concat(ClinicalParameterCatalog.BloodGas).Select(item => item.Id), StringComparer.Ordinal);
            return rules.Select((rule, ruleIndex) =>
            {
                string path = "$.DiagnosticExclusions[" + ruleIndex + "]";
                var conditions = rule as object[];
                if (conditions == null || conditions.Length == 0) throw Error(path, "esperada lista não vazia de condições");
                var parsed = conditions.Select((condition, index) =>
                {
                    string itemPath = path + "[" + index + "]";
                    var item = Object(condition, itemPath, "Parameter", "Min", "Max");
                    string parameter = Text(item, "Parameter", itemPath);
                    if (!ids.Contains(parameter)) throw Error(itemPath + ".Parameter", "parâmetro desconhecido");
                    double min = Numeric(Required(item, "Min", itemPath), itemPath + ".Min");
                    double max = Numeric(Required(item, "Max", itemPath), itemPath + ".Max");
                    if (min > max) throw Error(itemPath, "mínimo maior que máximo");
                    return new DiagnosticCondition(parameter, min, max);
                }).ToArray();
                if (parsed.Select(item => item.Parameter).Distinct(StringComparer.Ordinal).Count() != parsed.Length)
                    throw Error(path, "parâmetro duplicado na regra");
                return new DiagnosticExclusionRule(parsed);
            }).ToArray();
        }

        private static double Numeric(object value, string path)
        {
            if (!(value is int) && !(value is long) && !(value is decimal) && !(value is double))
                throw Error(path, "esperado número");
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || double.IsInfinity(number)) throw Error(path, "número inválido");
            return number;
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
