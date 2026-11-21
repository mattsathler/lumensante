using System;
using System.Collections.Generic;

namespace lumen_sante.Clinical
{
    internal sealed class PatientProfile
    {
        private static readonly string[] BloodTypes = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        private static readonly string[] PastExams =
        {
            "Hemograma de rotina",
            "Painel de eletrólitos",
            "Avaliação da função renal",
            "Avaliação da função hepática",
            "Glicemia de rotina"
        };

        internal PatientProfile(Random random)
        {
            Age = random.Next(18, 90);
            Phone = $"+1 (213) 555-{random.Next(100, 200):D4}";
            BloodType = BloodTypes[random.Next(BloodTypes.Length)];

            var priorExams = new List<string>(PastExams);
            var history = new PreviousExam[random.Next(2, 5)];
            int monthsAgo = 0;
            for (int index = 0; index < history.Length; index++)
            {
                monthsAgo += random.Next(3, 13);
                int pick = random.Next(priorExams.Count);
                history[index] = new PreviousExam(priorExams[pick], monthsAgo);
                priorExams.RemoveAt(pick);
            }

            MedicalHistory = Array.AsReadOnly(history);
        }

        internal int Age { get; }
        internal string Phone { get; }
        internal string BloodType { get; }
        internal IReadOnlyList<PreviousExam> MedicalHistory { get; }
    }

    internal readonly struct PreviousExam
    {
        internal PreviousExam(string name, int monthsAgo)
        {
            Name = name;
            MonthsAgo = monthsAgo;
        }

        internal string Name { get; }
        internal int MonthsAgo { get; }
    }
}
