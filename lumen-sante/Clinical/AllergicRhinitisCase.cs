namespace lumen_sante.Clinical
{
    internal static class AllergicRhinitisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "allergic-rhinitis", "Rinite alérgica", 0.025, 3, 6, SymptomaticProfile.Local,
            new[] { "Espirros e irritação nasal", "Obstrução nasal e cansaço", "Prostração com desconforto persistente" },
            new[] { new MedicationEffect(MedicationClass.Antihistamine, 0.85, 0.35), new MedicationEffect(MedicationClass.Corticosteroid, 0.75, 0.40) },
            new[]
            {
                new[] { "Meu nariz está escorrendo e tenho espirrado.", "Sinto o nariz coçar e a garganta arranhar.", "Acordei com o nariz entupido e os olhos irritados.", "Estou com coriza e um pouco de cansaço." },
                new[] { "O nariz entupido está atrapalhando meu sono.", "Tenho crises de espirros que me deixam cansado.", "A irritação no nariz e nos olhos não me dá descanso.", "A coriza está constante e me deixa indisposto." },
                new[] { "Estou muito debilitado e o nariz continua incomodando.", "Não consegui dormir e estou sem forças.", "A irritação continua e estou ficando tonto.", "Estou exausto, com o nariz entupido o tempo todo." }
            },
            ("Tem outros incômodos no rosto?", new[] { "Meus olhos coçam junto com o nariz.", "Os olhos lacrimejam quando começo a espirrar.", "Sinto a garganta arranhar quando o nariz escorre.", "Tenho uma sensação de irritação nos olhos e na garganta." }),
            ("Percebe algo que agrava os sintomas?", new[] { "Piora quando mexo com coisas empoeiradas.", "Começo a espirrar mais quando limpo a casa.", "Ficar perto de poeira aumenta bastante o incômodo.", "Quando saio de um ambiente empoeirado, sinto algum alívio." }));
    }
}
