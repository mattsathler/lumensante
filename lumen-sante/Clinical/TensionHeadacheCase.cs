namespace lumen_sante.Clinical
{
    internal static class TensionHeadacheCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "tension-headache", "Cefaleia tensional", 0.025, 3, 6, SymptomaticProfile.Local,
            new[] { "Desconforto na cabeça e tensão cervical", "Dor persistente e cansaço", "Prostração com dor persistente" },
            new[] { new MedicationEffect(MedicationClass.Analgesic, 0.85, 0.40), new MedicationEffect(MedicationClass.AntiInflammatory, 0.78, 0.50) },
            new[]
            {
                new[] { "Minha cabeça está pesada e estou cansado.", "Sinto um aperto na cabeça.", "Estou com dor na cabeça e no pescoço.", "A cabeça incomoda e está difícil me concentrar." },
                new[] { "O aperto na cabeça está mais persistente.", "Minha cabeça dói e o pescoço está muito tenso.", "A dor está me impedindo de trabalhar direito.", "Estou esgotado com essa pressão na cabeça." },
                new[] { "Estou sem forças e a cabeça continua doendo.", "O desconforto não me deixou descansar.", "Estou muito debilitado com essa dor persistente.", "Não consegui manter minhas atividades por causa do mal-estar." }
            },
            ("Como é a dor na cabeça?", new[] { "Parece uma faixa apertando os dois lados.", "É uma pressão dos dois lados, sem latejar.", "Sinto os músculos do pescoço doloridos junto.", "O couro cabeludo fica sensível quando encosto." }),
            ("Percebe alguma relação com sua rotina?", new[] { "Tenho dormido mal e ficado muito tenso.", "Ficar muito tempo na mesma posição incomoda o pescoço.", "A tensão aumenta depois de um dia estressante.", "Descansar em um ambiente tranquilo traz algum alívio." }));
    }
}
