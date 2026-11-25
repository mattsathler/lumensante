using System;

namespace lumen_sante.Clinical
{
    internal enum MedicationClass
    {
        Antibiotic, AntiInflammatory, Corticosteroid, Analgesic, Antipyretic,
        Antihistamine, Antiemetic, Bronchodilator, Antacid, OxygenTherapy
    }

    internal static class MedicationCatalog
    {
        internal static System.Collections.Generic.IReadOnlyList<(MedicationClass Class, string Name)> Options { get; } =
            Array.AsReadOnly(new[]
            {
                (MedicationClass.Antibiotic, "Antibióticos"),
                (MedicationClass.AntiInflammatory, "Anti-inflamatórios"),
                (MedicationClass.Corticosteroid, "Corticoides"),
                (MedicationClass.Analgesic, "Analgésicos"),
                (MedicationClass.Antipyretic, "Antitérmicos"),
                (MedicationClass.Antihistamine, "Antialérgicos"),
                (MedicationClass.Antiemetic, "Antieméticos"),
                (MedicationClass.Bronchodilator, "Broncodilatadores"),
                (MedicationClass.Antacid, "Antiácidos"),
                (MedicationClass.OxygenTherapy, "Oxigenoterapia")
            });
    }

    internal sealed class MedicationEffect
    {
        internal MedicationEffect(MedicationClass medication, double improvementChance, double worseningMultiplier)
        {
            if (double.IsNaN(improvementChance) || improvementChance < 0 || improvementChance > 1 ||
                double.IsNaN(worseningMultiplier) || worseningMultiplier < 0 || worseningMultiplier > 1)
                throw new ArgumentException("Efeito de medicamento inválido.");
            Medication = medication;
            ImprovementChance = improvementChance;
            WorseningMultiplier = worseningMultiplier;
        }

        internal MedicationClass Medication { get; }
        internal double ImprovementChance { get; }
        internal double WorseningMultiplier { get; }
    }
}
