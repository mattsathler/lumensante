namespace lumen_sante.Clinical
{
    internal static class ViralPharyngitisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "viral-pharyngitis", "Faringite viral", 0.055, 2, 5, SymptomaticProfile.Infection,
            new[] { "Desconforto ao engolir e voz rouca", "Dor ao engolir e abatimento", "Prostração e dificuldade de alimentação" },
            new[] { new MedicationEffect(MedicationClass.Analgesic, 0.78, 0.65), new MedicationEffect(MedicationClass.AntiInflammatory, 0.72, 0.55), new MedicationEffect(MedicationClass.Antipyretic, 0.65, 0.70) },
            new[]
            {
                new[] { "Minha garganta arranha e estou cansado.", "Sinto dor quando engulo e minha voz está diferente.", "Estou com a garganta seca e uma tosse leve.", "Minha garganta incomoda junto com uma indisposição." },
                new[] { "Engolir está doendo bastante.", "Minha garganta dói e estou comendo menos.", "A dor na garganta está atrapalhando meu sono.", "Estou rouco e falar ficou desconfortável." },
                new[] { "Estou fraco e quase não consigo me alimentar.", "A garganta dói muito e estou ficando tonto.", "Estou muito abatido e beber está difícil.", "Falar e engolir estão me deixando exausto." }
            },
            ("Como estão a voz e a tosse?", new[] { "Minha voz ficou rouca desde que isso começou.", "Tenho uma tosse seca que irrita mais a garganta.", "Falar por muito tempo aumenta a irritação.", "A garganta arranha antes de eu começar a tossir." }),
            ("O que piora ou alivia o incômodo?", new[] { "Comida seca incomoda mais ao engolir.", "Um gole de água alivia por alguns instantes.", "Líquidos mornos são mais confortáveis.", "Ficar sem falar por um tempo reduz a irritação." }),
            temperatureProfile: TemperatureProfile.MildFever);
    }
}
