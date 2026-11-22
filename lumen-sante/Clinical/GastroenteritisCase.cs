namespace lumen_sante.Clinical
{
    internal static class GastroenteritisCase
    {
        internal static readonly DiseaseDefinition Definition = SymptomaticCaseFactory.Create(
            "acute-gastroenteritis", "Gastroenterite aguda", 0.09, 2, 4, SymptomaticProfile.Digestive,
            new[] { "Náusea e desconforto abdominal", "Abatimento e sinais de perda de líquidos", "Prostração e desidratação acentuada" },
            new[] { new MedicationEffect(MedicationClass.Antiemetic, 0.78, 0.45) },
            new[]
            {
                new[] { "Estou enjoado e com dor na barriga.", "Meu estômago está embrulhado e estou indisposto.", "Tenho cólicas e pouca vontade de comer.", "Estou com um mal-estar e vontade de vomitar." },
                new[] { "O enjoo está me impedindo de comer direito.", "Estou com cólicas frequentes e muita fraqueza.", "As idas ao banheiro estão me deixando cansado.", "Estou com a boca seca e o estômago embrulhado." },
                new[] { "Estou muito fraco e quase não consigo beber.", "Estou ficando tonto quando tento levantar.", "A boca está seca e estou sem forças.", "Estou debilitado com o enjoo e a perda de líquidos." }
            },
            ("Teve vômito ou mudança no intestino?", new[] { "Tive episódios de vômito junto com a diarreia.", "Estou indo ao banheiro com fezes bem mais líquidas.", "As cólicas vêm antes de precisar ir ao banheiro.", "O enjoo vem em ondas e às vezes acabo vomitando." }),
            ("Como está tolerando comida e líquidos?", new[] { "Comida pesada aumenta minha vontade de vomitar.", "Tenho tentado tomar pequenos goles de água.", "Quando como muito de uma vez, o enjoo aumenta.", "Estou tolerando melhor pequenas quantidades do que uma refeição inteira." }),
            temperatureProfile: TemperatureProfile.MildFever);
    }
}
