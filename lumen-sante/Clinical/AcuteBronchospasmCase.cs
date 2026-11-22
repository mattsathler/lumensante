namespace lumen_sante.Clinical
{
    internal static class AcuteBronchospasmCase
    {
        // Resolves the acute episode in the simulation, without modelling a cure for chronic asthma.
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "acute-bronchospasm", "Broncoespasmo agudo", 0.12, 1, 3, SymptomaticProfile.Respiratory,
            new[] { "Tosse e desconforto respiratório", "Fala com pausas e esforço respiratório", "Exaustão e dificuldade respiratória importante" },
            new[] { new MedicationEffect(MedicationClass.Bronchodilator, 0.85, 0.30), new MedicationEffect(MedicationClass.Corticosteroid, 0.65, 0.45) },
            new[]
            {
                new[] { "Estou tossindo e com um aperto no peito.", "Minha respiração está fazendo um chiado.", "Sinto falta de ar quando faço esforço.", "Estou com tosse e dificuldade para soltar o ar." },
                new[] { "O aperto no peito está mais forte e fico ofegante.", "Tenho que parar para respirar quando caminho.", "O chiado está persistente e estou cansado.", "Conversar por muito tempo me deixa sem fôlego." },
                new[] { "Estou quase sem ar mesmo parado.", "Preciso interromper as frases para respirar.", "Meu peito está apertado e estou exausto.", "Respirar está exigindo muito esforço." }
            },
            ("Como estão a tosse e a respiração?", new[] { "Ouço um chiado quando solto o ar.", "Tenho uma tosse seca junto com o aperto no peito.", "Parece mais difícil soltar o ar do que antes.", "O peito fica apertado durante as crises de tosse." }),
            ("Percebe algo que agrava a respiração?", new[] { "Respirar ar com poeira aumenta a tosse.", "Fumaça deixa meu peito mais apertado.", "Fazer esforço aumenta o chiado e a falta de ar.", "Ar frio costuma incomodar mais a minha respiração." }));
    }
}
