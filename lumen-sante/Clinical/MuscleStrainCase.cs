namespace lumen_sante.Clinical
{
    internal static class MuscleStrainCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "muscle-strain", "Distensão muscular", 0.03, 3, 6, SymptomaticProfile.Local,
            new[] { "Dor muscular e movimento cauteloso", "Dor persistente e limitação de movimento", "Prostração com limitação funcional importante" },
            new[] { new MedicationEffect(MedicationClass.Analgesic, 0.78, 0.60), new MedicationEffect(MedicationClass.AntiInflammatory, 0.82, 0.40) },
            new[]
            {
                new[] { "Estou com dor muscular depois de um esforço.", "Senti uma fisgada e o músculo ficou dolorido.", "Uma região do corpo está dolorida quando me mexo.", "Estou com uma dor localizada e dificuldade para me movimentar." },
                new[] { "O músculo dói bastante quando tento me mexer.", "A dor está limitando meus movimentos.", "Estou evitando usar a região porque incomoda muito.", "A dor muscular está atrapalhando meu trabalho." },
                new[] { "Estou muito debilitado e mal consigo me movimentar.", "A dor continua e estou sem forças.", "Preciso de ajuda para me mover por causa do desconforto.", "Não consegui descansar com essa dor persistente." }
            },
            ("Como é a dor na região afetada?", new[] { "É uma dor localizada, e o músculo fica sensível ao toque.", "Sinto como se o músculo estivesse repuxando.", "A região fica dolorida quando tento contrair o músculo.", "Tenho uma sensação de rigidez junto com a dor." }),
            ("O que estava fazendo antes de a dor aparecer?", new[] { "Fiz um esforço maior do que estou acostumado.", "Levantei algo pesado e senti a fisgada.", "Forcei o músculo durante uma atividade física.", "A dor começou depois de um movimento brusco." }));
    }
}
