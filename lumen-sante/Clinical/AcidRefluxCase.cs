namespace lumen_sante.Clinical
{
    internal static class AcidRefluxCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "acid-reflux", "Episódio de refluxo gastroesofágico", 0.035, 2, 5, SymptomaticProfile.Digestive,
            new[] { "Desconforto na região superior do abdome", "Queimação persistente e indisposição", "Prostração com dificuldade de alimentação" },
            new[] { new MedicationEffect(MedicationClass.Antacid, 0.85, 0.40) },
            new[]
            {
                new[] { "Estou com uma queimação e um pouco de enjoo.", "Sinto um desconforto na boca do estômago.", "Meu peito arde depois que como.", "Estou com uma sensação ruim subindo pela garganta." },
                new[] { "A queimação está atrapalhando meu sono.", "O desconforto vem depois das refeições e está mais forte.", "A garganta fica irritada junto com essa ardência.", "Estou evitando comer porque a queimação incomoda." },
                new[] { "Estou muito fraco e quase não tenho conseguido comer.", "A queimação continua e estou debilitado.", "Estou ficando tonto e me alimentando pouco.", "Não consegui descansar com esse desconforto persistente." }
            },
            ("Sente algo na boca ou na garganta?", new[] { "Às vezes vem um gosto azedo na boca.", "Sinto um líquido amargo subir depois que como.", "Minha voz fica rouca junto com a irritação na garganta.", "Tenho uma tosse seca quando a garganta fica irritada." }),
            ("Quando a queimação incomoda mais?", new[] { "Piora quando me deito depois de comer.", "Comer uma refeição grande aumenta o incômodo.", "Me curvar depois de comer faz a ardência subir.", "Ficar sentado depois da refeição é mais confortável do que deitar." }));
    }
}
