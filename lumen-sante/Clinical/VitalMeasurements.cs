using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace lumen_sante.Clinical
{
    internal static class VitalMeasurements
    {
        internal static ExamOrders.SavedMeasurement[] Capture(PatientClinicalState state) =>
            CaptureParameters(state, ClinicalParameterCatalog.Vitals);

        internal static ExamOrders.SavedMeasurement[] CaptureParameters(PatientClinicalState state, IEnumerable<ClinicalParameterDefinition> parameters) =>
            parameters.Select(parameter => new ExamOrders.SavedMeasurement
            {
                Id = parameter.Id, Name = parameter.Name, Unit = parameter.Unit,
                Minimum = parameter.Minimum, Maximum = parameter.Maximum, Decimals = parameter.Decimals,
                Value = state.GetParameter(parameter.Id)
            }).ToArray();

        internal static string Describe(PatientClinicalState state) =>
            state.HasVitals ? FormatMeasurements(Capture(state)) : "Medições indisponíveis.";

        internal static string FormatMeasurements(ExamOrders.SavedMeasurement[] measurements)
        {
            var culture = CultureInfo.GetCultureInfo("pt-BR");
            return string.Join("\n", measurements.Select(item =>
            {
                if (!item.Value.HasValue) return item.Name + ": indisponível";
                string format = "F" + item.Decimals;
                return item.Name + ": " + item.Value.Value.ToString(format, culture) + " " + item.Unit +
                    " (referência: " + item.Minimum.ToString(format, culture) + "–" +
                    item.Maximum.ToString(format, culture) + " " + item.Unit + ")";
            }));
        }
    }
}
