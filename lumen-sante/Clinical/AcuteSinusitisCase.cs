namespace lumen_sante.Clinical
{
    internal static class AcuteSinusitisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "acute-sinusitis", "Rinossinusite aguda viral", 0.065, 3, 6, SymptomaticProfile.Infection,
            new[] { "Congestão nasal e desconforto facial", "Dor facial e abatimento", "Prostração com dor facial persistente" },
            new[] { new MedicationEffect(MedicationClass.Analgesic, 0.74, 0.65), new MedicationEffect(MedicationClass.AntiInflammatory, 0.76, 0.55), new MedicationEffect(MedicationClass.Corticosteroid, 0.70, 0.50) },
            new[]
            {
                new[] { "Estou com o nariz entupido e a cabeça pesada.", "Sinto pressão no rosto e um pouco de cansaço.", "Minha testa incomoda junto com a congestão.", "Estou com coriza e dor ao redor dos olhos." },
                new[] { "A pressão no rosto está ficando mais forte.", "Meu nariz não desentope e a cabeça dói bastante.", "A dor na testa está atrapalhando minhas atividades.", "Sinto o rosto pesado e estou muito indisposto." },
                new[] { "Estou muito abatido e a dor no rosto continua.", "Não consegui descansar por causa da dor de cabeça.", "A pressão no rosto não passa e estou sem forças.", "Estou ficando tonto com esse mal-estar persistente." }
            },
            ("Como estão o nariz e o olfato?", new[] { "Estou sentindo menos o cheiro das coisas.", "Meu nariz parece bloqueado por dentro.", "Tenho secreção nasal e uma sensação de pressão.", "A congestão deixa minha voz mais anasalada." }),
            ("O que agrava a dor no rosto?", new[] { "Abaixar a cabeça aumenta a pressão.", "Inclinar o corpo para frente faz meu rosto pesar.", "Sinto mais pressão quando me curvo.", "Quando fico inclinado para pegar algo, a testa incomoda mais." }),
            temperatureProfile: TemperatureProfile.MildFever);
    }
}
