namespace lumen_sante.Clinical
{
    using System.Linq;
    internal static class FluLikeCase
    {
        // Later stages represent complications in the game's compressed clinical timeline.
        private static readonly DiseaseStage[] Stages =
        {
            new DiseaseStage((76, 88), (112, 126), (70, 82), (14, 18), (97, 99),
                ("Alerta, com indisposição leve", "Tosse seca ocasional", "Sem alteração visível"), (370, 383)),
            new DiseaseStage((82, 94), (110, 124), (68, 80), (16, 20), (96, 99),
                ("Cansaço e desconforto", "Tosse intermitente", "Pele ruborizada"), (375, 387)),
            new DiseaseStage((88, 100), (108, 122), (66, 80), (18, 22), (95, 98),
                ("Aspecto abatido", "Tosse frequente", "Sudorese discreta"), (378, 390)),
            new DiseaseStage((94, 106), (104, 120), (64, 78), (20, 24), (93, 96),
                ("Prostração evidente", "Respiração acelerada", "Sudorese visível"), (380, 393)),
            new DiseaseStage((100, 114), (100, 116), (62, 74), (22, 26), (90, 94),
                ("Dificuldade para manter as atividades", "Esforço respiratório discreto", "Palidez discreta"), (382, 395)),
            new DiseaseStage((108, 120), (94, 110), (58, 70), (24, 28), (87, 91),
                ("Prostração intensa", "Esforço respiratório em repouso", "Palidez evidente"), (385, 398)),
            new DiseaseStage((114, 128), (86, 102), (54, 66), (27, 31), (83, 88),
                ("Fala entrecortada e exaustão", "Respiração trabalhosa", "Extremidades frias"), (387, 400)),
            new DiseaseStage((122, 136), (78, 94), (48, 60), (30, 35), (78, 84),
                ("Sonolência e resposta lenta", "Esforço respiratório intenso", "Lábios azulados"), (390, 402)),
            new DiseaseStage((128, 142), (68, 84), (40, 54), (32, 38), (70, 79),
                ("Resposta muito reduzida", "Respiração irregular", "Cianose evidente"), (392, 405)),
            new DiseaseStage((0, 0), (0, 0), (0, 0), (0, 0), (0, 0),
                ("Sem resposta", "Sem movimento respiratório", "—"))
        };

        private static readonly string[][] Symptoms =
        {
            new[] { "Estou tossindo e com o corpo dolorido.", "Minha garganta arranha e estou cansado.", "Comecei a sentir calafrios e dor de cabeça.", "Meu nariz está entupido e me sinto indisposto." },
            new[] { "A tosse está atrapalhando meu sono.", "As dores no corpo ficaram mais fortes.", "Estou alternando frio e suor.", "Minha garganta dói quando engulo e estou sem energia." },
            new[] { "Até levantar da cama me deixa cansado.", "Estou com dor de cabeça e pouca vontade de comer.", "A tosse está vindo em crises mais frequentes.", "Meu corpo parece pesado e não consigo trabalhar." },
            new[] { "Fico ofegante quando caminho pelo quarto.", "A tosse está acompanhada de muito cansaço.", "Estou suando e sem forças para fazer minhas coisas.", "Minha respiração parece mais rápida que antes." },
            new[] { "Preciso parar para respirar depois de poucos passos.", "Não tenho força para ficar muito tempo em pé.", "A tosse continua e meu peito está incomodando.", "Meu apetite sumiu e até conversar me cansa." },
            new[] { "Sinto falta de ar mesmo sentado.", "Estou exausto e respirar ficou trabalhoso.", "A tosse me deixa sem fôlego por bastante tempo.", "Minha fraqueza piorou e estou ficando tonto." },
            new[] { "Preciso interromper as frases para respirar.", "Falar me deixa muito cansado.", "Estou com as mãos frias e falta de ar.", "Parece que meu corpo ficou sem nenhuma força." },
            new[] { "Estou muito sonolento e quase sem ar.", "Tenho dificuldade para acompanhar a conversa.", "Até uma frase curta exige esforço.", "Minha respiração ficou pesada e estou confuso." },
            new[] { "Mal consigo... falar.", "Estou quase... sem ar.", "Não consigo... ficar acordado.", "Preciso de ajuda... para respirar." }
        };

        internal static readonly DiseaseDefinition Definition = new DiseaseDefinition(
            "flu-like", 20, 60 * 60000, 120 * 60000, Stages,
            worseningRisk: 0.18, treatmentSuccessRate: 0,
            name: "Síndrome gripal", treatmentName: null, requiresSpecificTreatment: false,
            medicationEffects: new[]
            {
                new MedicationEffect(MedicationClass.Analgesic, 0.65, 0.85),
                new MedicationEffect(MedicationClass.Antipyretic, 0.80, 0.75)
            },
            ("Como você está se sentindo?", stage => Responses(Symptoms[stage], stage < 3
                ? new[] { "Tenho dormido mal por causa disso.", "Perdi o apetite junto com esses sintomas.", "Sinto o corpo quente em alguns momentos.", "Minha cabeça pesa quando tento me concentrar.", "Minha garganta ficou mais sensível." }
                : new[] { "A fraqueza está atrapalhando até os cuidados pessoais.", "Minha respiração exige mais esforço que antes.", "Estou comendo menos desde que piorei.", "Preciso descansar depois de qualquer esforço.", "O sono deixou de me dar sensação de descanso." })),
            ("Os sintomas começaram há quanto tempo?", stage => Responses(
                new[] { "Tudo começou de repente há dois dias.", "Ontem acordei com essas dores e a tosse.",
                    "Fiquei indisposto de uma hora para outra nesta semana.", "Comecei a me sentir mal na noite de anteontem." },
                new[] { "No começo, achei que fosse só cansaço.", "A irritação na garganta apareceu junto.",
                    "Também comecei a espirrar nessa época.", "As dores no corpo vieram nas primeiras horas.", "Foi quando meu apetite diminuiu." })),
            ("Sente falta de ar?", stage => Responses(stage < 3
                ? new[] { "Em repouso respiro normalmente.", "Fico cansado quando caminho depressa.", "A tosse me faz parar um pouco para recuperar o fôlego.", "Subir escadas está mais cansativo que de costume." }
                : stage < 6
                    ? new[] { "Sim, até uma caminhada curta me deixa ofegante.", "Estou respirando mais rápido mesmo sem esforço.", "Preciso parar no meio das tarefas para respirar.", "A tosse vem e demora para meu fôlego voltar." }
                    : new[] { "Mesmo deitado está difícil respirar.", "Tenho que interromper as frases para buscar ar.", "Estou sem fôlego até quando fico parado.", "Minha respiração exige esforço o tempo todo." },
                stage < 3
                    ? new[] { "Melhora quando paro para descansar.", "Consigo conversar sem precisar fazer pausas.", "Ainda consigo beber água sem interromper para respirar.", "Minha respiração não está fazendo barulho.", "É um cansaço diferente do meu habitual." }
                    : new[] { "Também sinto o peito cansado.", "Estou tentando respirar mais fundo.", "A posição sentada está mais confortável para mim.", "Isso piorou junto com a fraqueza.", "Eu não tinha esse esforço para respirar antes de adoecer." })),
            ("Como está a tosse?", stage => Responses(stage < 4
                ? new[] { "É uma tosse seca que irrita minha garganta.", "Tosse sem catarro, principalmente quando falo.", "A tosse vem em acessos e atrapalha o sono.", "Minha garganta arranha antes de começar a tossir." }
                : new[] { "A tosse ficou mais forte e me deixa exausto.", "Sinto o peito incomodar durante as crises.", "Preciso recuperar o fôlego depois de tossir.", "A tosse piorou junto com a falta de ar." },
                new[] { "Não percebi sangue quando tossi.", "Minha voz ficou rouca desde que isso começou.",
                    "Um gole de água alivia a irritação por alguns instantes.", "Também tenho tido espirros.", "Meu nariz começou a escorrer junto com a tosse." })),
            ("Está conseguindo comer e beber?", stage => Responses(stage < 4
                ? new[] { "Estou com menos apetite, mas consigo beber água.", "A garganta incomoda ao engolir, então estou comendo menos.", "Não tive muita fome desde que comecei a ficar doente.", "Tenho bebido água aos poucos e comido refeições pequenas." }
                : new[] { "Estou fraco demais para comer normalmente.", "Meu apetite caiu bastante e estou bebendo pouco.", "Preciso fazer pausas até para tomar água.", "Estou com a boca seca e sem vontade de comer." },
                new[] { "Alimentos macios são mais confortáveis para engolir.", "Comida seca irrita minha garganta.",
                    "Não tive vômitos até agora.", "Estou sentindo menos o gosto dos alimentos com o nariz entupido.", "Tenho preferido líquidos mornos porque minha garganta está sensível." })));

        private static string[] Responses(string[] statements, string[] details) =>
            statements.SelectMany(statement => details.Select(detail => statement + " " + detail)).ToArray();
    }
}
