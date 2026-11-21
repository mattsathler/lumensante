namespace lumen_sante
{
    internal sealed class LungCancerCase
    {
        internal static readonly LungCancerCase[] Stages =
        {
            new LungCancerCase(78, 122, 78, 16, 98, "Tenho tossido com frequência."),
            new LungCancerCase(82, 124, 80, 17, 97, "A tosse piorou. Fico sem fôlego quando subo escadas."),
            new LungCancerCase(88, 124, 80, 18, 96, "A tosse continua e eu me canso com muito menos esforço."),
            new LungCancerCase(94, 122, 78, 20, 94, "Sinto um incômodo no peito quando tusso."),
            new LungCancerCase(99, 118, 76, 22, 92, "Estou com falta de ar quando caminho poucos metros."),
            new LungCancerCase(104, 114, 74, 24, 90, "Vi alguns fios de sangue no catarro. A tosse não passa."),
            new LungCancerCase(112, 108, 70, 27, 87, "Estou sem fôlego até em repouso e muito cansado."),
            new LungCancerCase(120, 98, 64, 30, 83, "Está difícil respirar. Preciso parar para recuperar o fôlego."),
            new LungCancerCase(128, 84, 54, 34, 77, "Quase não consigo respirar... nem falar direito."),
            new LungCancerCase(0, 0, 0, 0, 0, string.Empty)
        };

        private LungCancerCase(int pulse, int systolic, int diastolic, int respiratoryRate,
            int oxygenSaturation, string symptoms)
        {
            Pulse = pulse;
            Systolic = systolic;
            Diastolic = diastolic;
            RespiratoryRate = respiratoryRate;
            OxygenSaturation = oxygenSaturation;
            Symptoms = symptoms;
        }

        internal int Pulse { get; }
        internal int Systolic { get; }
        internal int Diastolic { get; }
        internal int RespiratoryRate { get; }
        internal int OxygenSaturation { get; }
        internal string Symptoms { get; }
    }
}
