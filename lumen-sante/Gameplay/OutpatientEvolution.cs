using System;
using System.Linq;
using lumen_sante.Clinical;

namespace lumen_sante.Gameplay
{
    internal static class OutpatientEvolution
    {
        internal static void Advance(MedicalAgenda.Record record, DiseaseDefinition disease, DateTime now, Random random)
        {
            if (disease == null || record.Deceased || !record.Orders.Discharged || record.Orders.Recovered ||
                record.NextEvolutionTicks <= 0 || record.NextEvolutionTicks > now.Ticks) return;
            var state = new PatientClinicalState();
            var saved = record.Orders.Vitals;
            if (saved != null) state.SetVitals((saved.Pulse, saved.Systolic, saved.Diastolic,
                saved.RespiratoryRate, saved.OxygenSaturation, saved.Temperature));
            else if (record.Vitals != null) state.SetVitals((record.Vitals.Pulse, record.Vitals.Systolic,
                record.Vitals.Diastolic, record.Vitals.RespiratoryRate, record.Vitals.OxygenSaturation, record.Vitals.Temperature));
            if (record.Orders.Leukocytes.HasValue) state.SetLeukocytes(record.Orders.Leukocytes.Value);
            state.SetNaturalRecoveryProgress(record.Orders.NaturalRecoveryProgress);
            state.RestoreSymptoms(record.Orders.Pain, record.Orders.Nausea, record.Orders.NasalCongestion);
            if (record.Orders.Chemistry != null)
                foreach (var item in record.Orders.Chemistry) state.SetChemistry(item.Id, item.Value);
            int risk = Math.Max(1, Math.Min(record.Risk, disease.Stages.Count - 1));
            while (record.NextEvolutionTicks <= now.Ticks && risk > 0 && risk < disease.Stages.Count)
            {
                var medication = record.Orders.Medications.Where(item => item.Expires > record.NextEvolutionTicks)
                    .Select(item => item.Class).ToArray();
                risk = state.AdvanceDisease(disease, risk, record.Orders.Treatment == disease.Id, medication, random);
                int stageIndex = Math.Max(0, risk - 1);
                if (risk < disease.Stages.Count && state.HasVitals)
                {
                    var stage = disease.Stages[stageIndex];
                    state.SetVitals(risk == 0 ? stage.AdvanceRecoveredVitals(state.Vitals) :
                        stage.AdvanceSupportedVitals(state.Vitals, medication.Contains(MedicationClass.Antipyretic),
                            medication.Contains(MedicationClass.OxygenTherapy)));
                }
                if (risk < disease.Stages.Count) state.AdvanceLeukocytes(disease, stageIndex, risk == 0);
                if (risk < disease.Stages.Count) state.AdvanceChemistry(disease, stageIndex, risk == 0);
                if (risk < disease.Stages.Count) state.AdvanceSymptoms(disease, stageIndex, risk == 0, medication);
                long increment = (long)random.Next(disease.MinimumStageDurationMilliseconds,
                    disease.MaximumStageDurationMilliseconds + 1) * TimeSpan.TicksPerMillisecond;
                record.NextEvolutionTicks = record.NextEvolutionTicks > DateTime.MaxValue.Ticks - increment
                    ? DateTime.MaxValue.Ticks : record.NextEvolutionTicks + increment;
            }
            record.Risk = risk;
            if (risk == 0) record.Orders.Recovered = true;
            if (risk == disease.Stages.Count) record.Deceased = true;
            record.Orders.Leukocytes = state.Leukocytes;
            record.Orders.NaturalRecoveryProgress = state.NaturalRecoveryProgress;
            record.Orders.Pain = state.PainIntensity;
            record.Orders.Nausea = state.NauseaIntensity;
            record.Orders.NasalCongestion = state.NasalCongestionIntensity;
            record.Orders.Chemistry = state.Chemistry.Select(item => new ExamOrders.SavedParameter { Id = item.Key, Value = item.Value }).ToArray();
            if (state.HasVitals)
            {
                var vitals = state.Vitals;
                record.Orders.Vitals = new ExamOrders.SavedVitals { Pulse = vitals.Pulse, Systolic = vitals.Systolic,
                    Diastolic = vitals.Diastolic, RespiratoryRate = vitals.RespiratoryRate,
                    OxygenSaturation = vitals.OxygenSaturation, Temperature = vitals.Temperature };
            }
        }
    }
}
