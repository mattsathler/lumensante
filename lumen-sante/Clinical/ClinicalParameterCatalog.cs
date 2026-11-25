using System;
using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed class ClinicalParameterDefinition
    {
        internal ClinicalParameterDefinition(string id, string name, string unit, double minimum, double maximum, int decimals)
        { Id = id; Name = name; Unit = unit; Minimum = minimum; Maximum = maximum; Decimals = decimals; }
        internal string Id { get; }
        internal string Name { get; }
        internal string Unit { get; }
        internal double Minimum { get; }
        internal double Maximum { get; }
        internal int Decimals { get; }
    }

    internal static class ClinicalParameterCatalog
    {
        internal static IReadOnlyList<ClinicalParameterDefinition> Vitals { get; } = Array.AsReadOnly(new[]
        {
            new ClinicalParameterDefinition("pulse", "Pulso", "bpm", 60, 100, 0),
            new ClinicalParameterDefinition("systolic-pressure", "Pressão sistólica", "mmHg", 90, 120, 0),
            new ClinicalParameterDefinition("diastolic-pressure", "Pressão diastólica", "mmHg", 60, 80, 0),
            new ClinicalParameterDefinition("respiratory-rate", "Frequência respiratória", "irpm", 12, 20, 0),
            new ClinicalParameterDefinition("oxygen-saturation", "Saturação de oxigênio", "%", 95, 100, 0),
            new ClinicalParameterDefinition("temperature", "Temperatura", "°C", 36, 37.4, 1)
        });
        internal static IReadOnlyList<ClinicalParameterDefinition> Hematology { get; } = Array.AsReadOnly(new[]
        {
            new ClinicalParameterDefinition("leukocytes", "Leucócitos", "/µL", 4000, 11000, 0),
            new ClinicalParameterDefinition("hemoglobin", "Hemoglobina", "g/dL", 12, 16, 1),
            new ClinicalParameterDefinition("platelets", "Plaquetas", "/µL", 150000, 450000, 0)
        });
        internal static IReadOnlyList<ClinicalParameterDefinition> Chemistry { get; } = Array.AsReadOnly(new[]
        {
            new ClinicalParameterDefinition("sodium", "Sódio", "mmol/L", 135, 145, 0),
            new ClinicalParameterDefinition("creatinine", "Creatinina", "mg/dL", 0.6, 1.3, 1),
            new ClinicalParameterDefinition("urine-leukocytes", "Leucócitos na urina", "/µL", 0, 25, 0),
            new ClinicalParameterDefinition("urine-erythrocytes", "Hemácias na urina", "/µL", 0, 10, 0),
            new ClinicalParameterDefinition("crp", "Proteína C reativa", "mg/L", 0, 4.9, 1),
            new ClinicalParameterDefinition("bun", "Nitrogênio ureico", "mg/dL", 6, 24, 0),
            new ClinicalParameterDefinition("glucose", "Glicose casual", "mg/dL", 70, 140, 0),
            new ClinicalParameterDefinition("urine-culture-count", "Contagem bacteriana urinária", "UFC/mL", 0, 9999, 0),
            new ClinicalParameterDefinition("urine-pbg", "Porfobilinogênio urinário", "mmol/mol creatinina", 0, 0.2, 2),
            new ClinicalParameterDefinition("ast", "AST", "U/L", 8, 48, 0),
            new ClinicalParameterDefinition("alt", "ALT", "U/L", 7, 55, 0),
            new ClinicalParameterDefinition("albumin", "Albumina", "g/dL", 3.5, 5.0, 1),
            new ClinicalParameterDefinition("bilirubin", "Bilirrubina total", "mg/dL", 0, 1.2, 1),
            new ClinicalParameterDefinition("inr", "INR", "razão", 0.9, 1.1, 1),
            new ClinicalParameterDefinition("lung-opacities", "Áreas pulmonares opacas", "áreas", 0, 0, 0),
            new ClinicalParameterDefinition("lung-nodules", "Nódulos pulmonares", "nódulos", 0, 0, 0),
            new ClinicalParameterDefinition("arterial-paco2", "Pressão arterial de CO₂", "mmHg", 35, 45, 0),
            new ClinicalParameterDefinition("arterial-ph", "pH arterial", "pH", 7.35, 7.45, 2),
            new ClinicalParameterDefinition("bicarbonate", "Bicarbonato", "mmol/L", 22, 26, 0),
            new ClinicalParameterDefinition("intrathoracic-lesions", "Lesões focais torácicas", "lesões", 0, 0, 0),
            new ClinicalParameterDefinition("intracranial-lesions", "Lesões focais intracranianas", "lesões", 0, 0, 0),
            new ClinicalParameterDefinition("spinal-lesions", "Lesões focais da coluna", "lesões", 0, 0, 0),
            new ClinicalParameterDefinition("ecg-abnormalities", "Alterações do traçado", "achados", 0, 0, 0)
        });
        internal static IReadOnlyList<ClinicalParameterDefinition> BloodGas { get; } = Array.AsReadOnly(new[]
        {
            new ClinicalParameterDefinition("arterial-pao2", "Pressão arterial de O₂", "mmHg", 80, 100, 0),
            Chemistry.First(item => item.Id == "arterial-paco2"),
            Chemistry.First(item => item.Id == "arterial-ph"),
            Chemistry.First(item => item.Id == "bicarbonate")
        });
        internal static IReadOnlyList<ClinicalParameterDefinition> ForExam(string examId)
        {
            switch (examId)
            {
                case "vital-signs": return Vitals;
                case "cardiac": return Chemistry.Where(item => item.Id == "ecg-abnormalities").ToArray();
                case "blood-gas": return BloodGas;
                case "hematology": return Hematology;
                case "electrolytes": return Chemistry.Where(item => item.Id == "sodium").ToArray();
                case "creatinine": return Chemistry.Where(item => item.Id == "creatinine").ToArray();
                case "urinalysis": return Chemistry.Where(item => item.Id == "urine-leukocytes" || item.Id == "urine-erythrocytes").ToArray();
                case "crp": return Chemistry.Where(item => item.Id == "crp").ToArray();
                case "urea": return Chemistry.Where(item => item.Id == "bun").ToArray();
                case "renal": return Chemistry.Where(item => item.Id == "bun" || item.Id == "creatinine").ToArray();
                case "glucose": return Chemistry.Where(item => item.Id == "glucose").ToArray();
                case "urine-culture": return Chemistry.Where(item => item.Id == "urine-culture-count").ToArray();
                case "urine-pbg": return Chemistry.Where(item => item.Id == "urine-pbg").ToArray();
                case "hepatic": return Chemistry.Where(item => item.Id == "ast" || item.Id == "alt" || item.Id == "albumin" || item.Id == "bilirubin").ToArray();
                case "albumin": return Chemistry.Where(item => item.Id == "albumin").ToArray();
                case "bilirubin": return Chemistry.Where(item => item.Id == "bilirubin").ToArray();
                case "coagulation": return Chemistry.Where(item => item.Id == "inr").ToArray();
                case "x-ray": return Chemistry.Where(item => item.Id == "lung-opacities" || item.Id == "lung-nodules").ToArray();
                case "mri": return Chemistry.Where(item => item.Id == "intrathoracic-lesions").ToArray();
                case "mri-brain": return Chemistry.Where(item => item.Id == "intracranial-lesions").ToArray();
                case "mri-spine": return Chemistry.Where(item => item.Id == "spinal-lesions").ToArray();
                default: return Array.Empty<ClinicalParameterDefinition>();
            }
        }
    }
}
