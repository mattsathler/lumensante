namespace lumen_sante.Clinical
{
    internal static class ContactDermatitisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "contact-dermatitis", "Dermatite de contato", 0.03, 3, 6, SymptomaticProfile.Skin,
            new[] { "Prurido e irritação cutânea localizada", "Prurido intenso e desconforto persistente", "Prostração com lesões cutâneas extensas" },
            new[] { new MedicationEffect(MedicationClass.Corticosteroid, 0.82, 0.35), new MedicationEffect(MedicationClass.Antihistamine, 0.62, 0.65) },
            new[]
            {
                new[] { "Minha pele está coçando e ardendo.", "Apareceram áreas vermelhas que incomodam.", "Estou com a pele irritada e sensível.", "A coceira está me deixando inquieto." },
                new[] { "A coceira está atrapalhando meu sono.", "A pele está muito irritada e tenho me coçado bastante.", "O ardor na pele está ficando mais incômodo.", "A irritação está dificultando minhas atividades." },
                new[] { "Estou exausto e a pele continua ardendo.", "As áreas irritadas incomodam muito e estou sem forças.", "Não consegui descansar por causa da coceira.", "Estou muito debilitado com esse desconforto na pele." }
            },
            ("Como está a pele nas áreas que incomodam?", new[] { "A pele ficou vermelha e ressecada.", "As áreas que coçam estão ásperas ao toque.", "Sinto ardor onde a pele está irritada.", "A pele parece rachada em alguns pontos que coçam." }),
            ("Percebeu contato com algo diferente?", new[] { "Começou depois que usei um produto de limpeza diferente.", "Mexi com um produto novo antes de a pele irritar.", "As áreas que tocaram o produto ficaram mais sensíveis.", "A irritação aumenta quando volto a usar aquele produto." }));
    }
}
