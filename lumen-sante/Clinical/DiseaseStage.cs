using System;

namespace lumen_sante.Clinical
{
    internal sealed class DiseaseStage
    {
        internal DiseaseStage((int Min, int Max) pulse, (int Min, int Max) systolic,
            (int Min, int Max) diastolic, (int Min, int Max) respiratoryRate,
            (int Min, int Max) oxygenSaturation, (string General, string Breathing, string Skin) inspection,
            (int Min, int Max)? temperatureTenths = null)
        {
            Pulse = pulse;
            Systolic = systolic;
            Diastolic = diastolic;
            RespiratoryRate = respiratoryRate;
            OxygenSaturation = oxygenSaturation;
            Inspection = inspection;
            // Integer tenths keep sampling inclusive and readings at one decimal place.
            TemperatureTenths = pulse.Max == 0 ? (0, 0) : temperatureTenths ?? (360, 374);
            if (TemperatureTenths.Min > TemperatureTenths.Max ||
                (pulse.Max != 0 && (TemperatureTenths.Min < 300 || TemperatureTenths.Max > 430)))
                throw new ArgumentOutOfRangeException(nameof(temperatureTenths));
        }

        internal (int Min, int Max) Pulse { get; }
        internal (int Min, int Max) Systolic { get; }
        internal (int Min, int Max) Diastolic { get; }
        internal (int Min, int Max) RespiratoryRate { get; }
        internal (int Min, int Max) OxygenSaturation { get; }
        internal (int Min, int Max) TemperatureTenths { get; }
        internal (string General, string Breathing, string Skin) Inspection { get; }

        internal int SamplePulse(Random random) => random.Next(Pulse.Min, Pulse.Max + 1);

        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature)
            SampleVitals(Random random, bool recovered = false)
        {
            int Sample((int Min, int Max) range) => random.Next(range.Min, range.Max + 1);

            return (SamplePulse(random), Sample(Systolic), Sample(Diastolic),
                Sample(RespiratoryRate), Sample(OxygenSaturation),
                TemperatureTenths.Max == 0 ? (double?)null : Sample(recovered ? (360, 374) : TemperatureTenths) / 10d);
        }
    }
}
