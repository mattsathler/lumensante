namespace lumen_sante.Clinical
{
    internal static class CystitisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "bacterial-cystitis", "Cistite bacteriana", 0.10, 2, 5, SymptomaticProfile.Infection,
            new[] { "Desconforto no baixo ventre", "Dor abdominal baixa e abatimento", "Prostração com sinais de comprometimento sistêmico" },
            new[] { new MedicationEffect(MedicationClass.Antibiotic, 0.82, 0.35) },
            new[]
            {
                new[] { "Sinto um incômodo embaixo da barriga.", "Estou indisposto e ir ao banheiro está desconfortável.", "Tenho uma pressão no baixo ventre.", "Estou com um mal-estar e desconforto ao urinar." },
                new[] { "A dor embaixo da barriga está mais forte.", "O desconforto ao urinar não me deixa descansar.", "Estou cansado e preciso ir ao banheiro toda hora.", "A pressão no baixo ventre está atrapalhando meu dia." },
                new[] { "Estou muito fraco e com dor na barriga.", "O mal-estar piorou e estou ficando tonto.", "Estou debilitado e continuo com desconforto ao urinar.", "Não tenho forças e a dor abdominal continua." }
            },
            ("Notou alguma mudança ao urinar?", new[] { "Sinto ardência quando a urina sai.", "Preciso urinar mais vezes, mas sai pouca quantidade.", "A vontade vem de repente e incomoda bastante.", "A urina parece mais turva e sinto ardor." }),
            ("Quando o desconforto aparece?", new[] { "A ardência é mais perceptível quando vou ao banheiro.", "A pressão incomoda quando a bexiga parece cheia.", "Tenho vontade de urinar de novo logo depois de terminar.", "Estou acordando para ir ao banheiro mais do que antes." }),
            temperatureProfile: TemperatureProfile.ComplicatedInfection);
    }
}
