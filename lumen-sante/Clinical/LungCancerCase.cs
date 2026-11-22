namespace lumen_sante.Clinical
{
    internal static class LungCancerCase
    {
        internal static readonly DiseaseStage[] Stages =
        {
            new DiseaseStage((74, 82), (117, 127), (75, 82), (14, 17), (97, 99),
                ("Alerta e confortável", "Sem esforço visível", "Sem alteração visível")),
            new DiseaseStage((78, 86), (119, 129), (76, 84), (15, 19), (96, 98),
                ("Alerta e confortável", "Tosse ocasional", "Sem alteração visível")),
            new DiseaseStage((84, 92), (119, 129), (76, 84), (16, 20), (95, 97),
                ("Aparência cansada", "Tosse intermitente", "Sem alteração visível")),
            new DiseaseStage((90, 98), (117, 127), (74, 82), (18, 22), (93, 96),
                ("Expressão de desconforto", "Respiração mais rápida", "Sem alteração visível")),
            new DiseaseStage((95, 103), (113, 123), (72, 80), (20, 24), (91, 94),
                ("Cansaço evidente", "Esforço discreto", "Palidez discreta")),
            new DiseaseStage((100, 108), (109, 119), (70, 78), (22, 26), (89, 92),
                ("Aparência abatida", "Esforço em repouso", "Palidez discreta")),
            new DiseaseStage((107, 117), (103, 113), (66, 74), (25, 29), (85, 89),
                ("Fala entrecortada", "Esforço visível no pescoço", "Palidez evidente")),
            new DiseaseStage((115, 125), (93, 103), (60, 68), (28, 32), (81, 85),
                ("Exaustão visível", "Esforço intenso", "Lábios azulados")),
            new DiseaseStage((122, 134), (79, 89), (50, 58), (32, 36), (75, 80),
                ("Sonolência evidente", "Respiração irregular", "Lábios azulados")),
            new DiseaseStage((0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
                ("Sem resposta", "Sem movimento respiratório", "—"))
        };

        private static readonly string[][] SymptomsByStage =
        {
            new[] { "Tenho tossido com frequência.", "A tosse vai e volta durante o dia.", "Comecei a tossir bem mais que de costume.", "A tosse aparece quase todo dia." },
            new[] { "A tosse piorou e fico sem fôlego nas escadas.", "Subir um lance de escada me deixa ofegante.", "Tosso mais à noite e me canso quando faço esforço.", "Até subir para casa no fim do dia ficou mais cansativo." },
            new[] { "Canso com muito menos esforço agora.", "Não consigo andar no ritmo de antes sem parar.", "A tosse continua e coisas simples me cansam.", "Tenho que fazer pausas nas tarefas de casa." },
            new[] { "Sinto um incômodo no peito quando tusso.", "Quando a tosse vem forte, o peito dói.", "A tosse deixa meu peito dolorido.", "Sinto uma pontada no peito durante as crises de tosse." },
            new[] { "Fico sem ar depois de caminhar poucos metros.", "Preciso parar para respirar mesmo em trajetos curtos.", "Atravessei o corredor e já fiquei ofegante.", "A tosse piorou e andar até a porta me cansa." },
            new[] { "Vi fios de sangue no catarro.", "Quando tossi, notei umas estrias de sangue.", "Além da falta de ar, apareceu um pouco de sangue na tosse.", "A tosse não passa e às vezes o catarro vem com sangue." },
            new[] { "Estou sem fôlego até em repouso.", "Mesmo deitado, sinto falta de ar.", "Estou muito cansado e respirar exige esforço.", "Só de ficar sentado já me sinto ofegante." },
            new[] { "Está difícil respirar mesmo parado.", "Preciso fazer pausas para recuperar o fôlego.", "Ultimamente até falar frases longas me deixa ofegante.", "O cansaço está forte e o ar parece não entrar direito." },
            new[] { "Mal consigo respirar.", "Está difícil... até falar.", "Estou sem ar... mesmo aqui na cama.", "Preciso parar... para respirar." }
        };

        private static readonly string[] Onset =
        {
            "Comecei a tossir há algumas semanas.", "Já faz algumas semanas que isso começou.",
            "Notei a tosse semanas atrás e achei que fosse passar.", "A tosse apareceu há semanas e continuou."
        };

        private static readonly string[] WeightLoss =
        {
            "Minhas roupas ficaram mais folgadas no último mês.", "Perdi peso sem mudar minha rotina.",
            "Meu peso baixou mesmo sem eu tentar emagrecer.", "Sim, percebi que emagreci nas últimas semanas."
        };

        internal static readonly DiseaseDefinition Definition = new DiseaseDefinition(
            "lung-cancer", 5, 2 * 60 * 60 * 1000, 4 * 60 * 60 * 1000, Stages,
            worseningRisk: 0.55, treatmentSuccessRate: 0.40,
            name: "Câncer de Pulmão", treatmentName: "Terapia oncológica",
            ("Como está se sentindo?", stage => PatientDialogue.Vary(SymptomsByStage[stage])),
            ("Os sintomas começaram há quanto tempo?", stage => PatientDialogue.Vary(Onset)),
            ("Sente falta de ar?", stage => PatientDialogue.Vary(stage < 2
                ? new[] { "Só quando faço esforço.", "Só quando subo escadas.", "Sim, se ando depressa.", "Sentado eu respiro bem, mas esforço me deixa ofegante." }
                : stage < 6
                    ? new[] { "Sim, até em caminhadas curtas.", "Fico ofegante andando pelo corredor.", "Preciso fazer pausas se ando um pouco.", "Sim, até para ir de um cômodo a outro." }
                    : new[] { "Sim, mesmo sem sair da cama.", "Mesmo em repouso fico ofegante.", "Falta ar até quando estou parado.", "Sim, preciso fazer esforço para respirar deitado." })),
            ("Notou sangue ao tossir?", stage => PatientDialogue.Vary(stage < 5
                ? new[] { "Até agora, não.", "Não reparei em sangue.", "Não vi sangue no catarro.", "Tossi bastante, mas não notei sangue." }
                : new[] { "Sim, vi sangue misturado ao catarro.", "Vi uns fios de sangue quando tossi.", "Há pouco, o catarro veio com sangue.", "Em algumas crises aparece um pouco de sangue." })),
            ("Perdeu peso recentemente?", stage => PatientDialogue.Vary(WeightLoss)));
    }
}
