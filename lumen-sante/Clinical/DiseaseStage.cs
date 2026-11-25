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

        private static int Advance(int value, (int Min, int Max) range)
        {
            int target = Math.Max(range.Min, Math.Min(range.Max, value));
            int difference = target - value;
            return value + Math.Sign(difference) * ((Math.Abs(difference) + 3) / 4);
        }

        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature)
            AdvanceRecoveredVitals((int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) current)
        {
            int temperature = current.Temperature.HasValue
                ? (int)Math.Round(current.Temperature.Value * 10, MidpointRounding.AwayFromZero) : 367;
            return (Advance(current.Pulse, Pulse), Advance(current.Systolic, Systolic),
                Advance(current.Diastolic, Diastolic), Advance(current.RespiratoryRate, RespiratoryRate),
                Advance(current.OxygenSaturation, OxygenSaturation), Advance(temperature, (360, 374)) / 10d);
        }

        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature)
            AdvanceVitals((int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) current)
            => AdvanceTreatedVitals(current, false);

        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature)
            AdvanceTreatedVitals((int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) current,
                bool antipyretic)
            => AdvanceSupportedVitals(current, antipyretic, false);

        internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature)
            AdvanceSupportedVitals((int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) current,
                bool antipyretic, bool oxygenTherapy)
        {
            if (Pulse.Max == 0) return (0, 0, 0, 0, 0, null);
            int temperature = current.Temperature.HasValue
                ? (int)Math.Round(current.Temperature.Value * 10, MidpointRounding.AwayFromZero)
                : TemperatureTenths.Min;
            var temperatureTarget = antipyretic && TemperatureTenths.Min > 374
                ? (Min: 360, Max: 374)
                : antipyretic ? (Min: TemperatureTenths.Min, Max: Math.Min(374, TemperatureTenths.Max)) : TemperatureTenths;
            var oxygenTarget = oxygenTherapy ? (Min: Math.Max(95, OxygenSaturation.Min), Max: Math.Max(95, OxygenSaturation.Max)) : OxygenSaturation;
            return (Advance(current.Pulse, Pulse), Advance(current.Systolic, Systolic),
                Advance(current.Diastolic, Diastolic), Advance(current.RespiratoryRate, RespiratoryRate),
                Advance(current.OxygenSaturation, oxygenTarget), Advance(temperature, temperatureTarget) / 10d);
        }

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
