using System.Linq;

namespace lumen_sante.Clinical
{
    // Dynamic readings belong to the clinical episode, not to its GTA entity.
    internal sealed class PatientClinicalState
    {
        private readonly System.Collections.Generic.Dictionary<string, double> chemistry = new System.Collections.Generic.Dictionary<string, double>(System.StringComparer.Ordinal);
        internal System.Collections.Generic.IReadOnlyDictionary<string, double> Chemistry => chemistry;
        internal void SetChemistry(string id, double value) { chemistry[id] = value; }
        internal void InitializeChemistry(DiseaseDefinition disease, int stage, System.Random random)
        {
            if (disease == null) return;
            foreach (var parameter in ClinicalParameterCatalog.Chemistry.Concat(ClinicalParameterCatalog.Hematology.Where(item => item.Id != "leukocytes")))
            {
                if (chemistry.ContainsKey(parameter.Id)) continue;
                var range = disease.ParameterRanges.TryGetValue(parameter.Id, out var ranges)
                    ? ranges[System.Math.Min(stage, ranges.Length - 1)] : (parameter.Minimum, parameter.Maximum);
                chemistry[parameter.Id] = System.Math.Round(range.Item1 + random.NextDouble() * (range.Item2 - range.Item1), parameter.Decimals);
            }
        }
        internal void AdvanceChemistry(DiseaseDefinition disease, int stage, bool recovered)
        {
            foreach (var parameter in ClinicalParameterCatalog.Chemistry.Concat(ClinicalParameterCatalog.Hematology.Where(item => item.Id != "leukocytes")))
            {
                if (!chemistry.TryGetValue(parameter.Id, out double value)) continue;
                var range = !recovered && disease.ParameterRanges.TryGetValue(parameter.Id, out var ranges)
                    ? ranges[System.Math.Min(stage, ranges.Length - 1)] : (parameter.Minimum, parameter.Maximum);
                double target = System.Math.Max(range.Item1, System.Math.Min(range.Item2, value));
                double distance = target - value;
                if (distance == 0) continue;
                double step = System.Math.Max(System.Math.Pow(10, -parameter.Decimals), System.Math.Abs(distance) / 4);
                chemistry[parameter.Id] = System.Math.Round(value + System.Math.Sign(distance) * System.Math.Min(step, System.Math.Abs(distance)), parameter.Decimals);
            }
        }
        internal bool HasVitals { get; private set; }
        internal long SymptomRevision { get; private set; }
        internal int? PainIntensity { get; private set; }
        internal int? NauseaIntensity { get; private set; }
        internal int? NasalCongestionIntensity { get; private set; }
        internal void RestoreSymptoms(int? pain, int? nausea, int? congestion) { PainIntensity = pain; NauseaIntensity = nausea; NasalCongestionIntensity = congestion; }
        internal void InitializeSymptoms(DiseaseDefinition disease, int stage, bool recovered)
        {
            if (disease == null) return;
            int initial = recovered ? 0 : System.Math.Min(10, stage + 2);
            if (!PainIntensity.HasValue && disease.SymptomIds.Contains("pain"))
                PainIntensity = initial;
            if (!NauseaIntensity.HasValue && disease.SymptomIds.Contains("nausea"))
                NauseaIntensity = initial;
            if (!NasalCongestionIntensity.HasValue && disease.SymptomIds.Contains("nasal-congestion"))
                NasalCongestionIntensity = initial;
        }
        internal void AdvanceSymptoms(DiseaseDefinition disease, int stage, bool recovered, System.Collections.Generic.IEnumerable<MedicationClass> medications)
        {
            InitializeSymptoms(disease, stage, recovered);
            int baseline = recovered ? 0 : System.Math.Min(10, stage + 2);
            int painRelief = medications.Contains(MedicationClass.Analgesic) ? 4 : medications.Contains(MedicationClass.AntiInflammatory) ? 3 : 0;
            int nauseaRelief = medications.Contains(MedicationClass.Antiemetic) ? 4 : 0;
            int congestionRelief = medications.Contains(MedicationClass.Antihistamine) ? 4 : medications.Contains(MedicationClass.Corticosteroid) ? 3 : 0;
            int? Move(int? current, int target) => current.HasValue
                ? current.Value + System.Math.Sign(target - current.Value) : (int?)null;
            int? newPain = Move(PainIntensity, System.Math.Max(0, baseline - painRelief));
            int? newNausea = Move(NauseaIntensity, System.Math.Max(0, baseline - nauseaRelief));
            int? newCongestion = Move(NasalCongestionIntensity, System.Math.Max(0, baseline - congestionRelief));
            if (newPain != PainIntensity || newNausea != NauseaIntensity || newCongestion != NasalCongestionIntensity) SymptomRevision++;
            PainIntensity = newPain; NauseaIntensity = newNausea; NasalCongestionIntensity = newCongestion;
        }
        internal static string DescribeIntensity(int intensity) => intensity <= 0 ? "Ausente no momento." :
            intensity <= 3 ? "Leve." : intensity <= 6 ? "Moderada." : "Intensa.";
        internal int? Leukocytes { get; private set; }
        internal int NaturalRecoveryProgress { get; private set; }
        internal void SetNaturalRecoveryProgress(int progress) { NaturalRecoveryProgress = progress; }
        internal int AdvanceDisease(DiseaseDefinition disease, int risk, bool correctTreatment,
            System.Collections.Generic.IEnumerable<MedicationClass> medications, System.Random random)
        {
            int next = disease.Evolve(risk, correctTreatment, medications, random);
            if (disease.NaturalRecoveryIntervals == 0 || risk == 0 || risk >= disease.Stages.Count) return next;
            if (next != risk) { NaturalRecoveryProgress = 0; return next; }
            NaturalRecoveryProgress++;
            if (NaturalRecoveryProgress < disease.NaturalRecoveryIntervals) return next;
            NaturalRecoveryProgress = 0;
            return risk - 1;
        }
        internal void SetLeukocytes(int value) { Leukocytes = value; }
        internal void InitializeLeukocytes(DiseaseDefinition disease, int stage, System.Random random)
        {
            if (Leukocytes.HasValue || disease == null || disease.LeukocyteRanges.Count == 0) return;
            var range = disease.LeukocyteRanges[System.Math.Min(stage, disease.LeukocyteRanges.Count - 1)];
            Leukocytes = random.Next(range.Min, range.Max + 1);
        }

        internal void AdvanceLeukocytes(DiseaseDefinition disease, int stage, bool recovered)
        {
            if (!Leukocytes.HasValue || disease.LeukocyteRanges.Count == 0) return;
            var range = recovered ? (Min: 4000, Max: 11000) :
                disease.LeukocyteRanges[System.Math.Min(stage, disease.LeukocyteRanges.Count - 1)];
            int current = Leukocytes.Value;
            int target = System.Math.Max(range.Min, System.Math.Min(range.Max, current));
            int difference = target - current;
            Leukocytes = current + System.Math.Sign(difference) * ((System.Math.Abs(difference) + 3) / 4);
        }
        internal int BreathlessnessIntensity => !HasVitals ? 0 : System.Math.Max(0, System.Math.Min(10, 95 - Vitals.OxygenSaturation));
        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) Vitals { get; private set; }

        internal double? GetParameter(string id)
        {
            if (id == "leukocytes") return Leukocytes;
            if (chemistry.TryGetValue(id, out double chemicalValue)) return chemicalValue;
            if (!HasVitals) return null;
            switch (id)
            {
                case "arterial-pao2": return 4 * Vitals.OxygenSaturation - 300;
                case "pulse": return Vitals.Pulse;
                case "systolic-pressure": return Vitals.Systolic;
                case "diastolic-pressure": return Vitals.Diastolic;
                case "respiratory-rate": return Vitals.RespiratoryRate;
                case "oxygen-saturation": return Vitals.OxygenSaturation;
                case "temperature": return Vitals.Temperature;
                default: return null;
            }
        }

        internal void SetVitals((int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) vitals)
        {
            int previousBreathlessness = BreathlessnessIntensity;
            Vitals = vitals;
            HasVitals = true;
            if (BreathlessnessIntensity != previousBreathlessness) SymptomRevision++;
        }

        internal string BreathlessnessDescription => BreathlessnessIntensity == 0 ? "Sem falta de ar no momento." :
            BreathlessnessIntensity <= 3 ? "Falta de ar leve." :
            BreathlessnessIntensity <= 6 ? "Falta de ar moderada." : "Falta de ar intensa.";
    }
}
