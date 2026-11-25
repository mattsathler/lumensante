using System;
using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal static class DiagnosticInvestigation
    {
        internal static IReadOnlyDictionary<string, double> LatestMeasurements(ExamOrders orders)
            => Measurements(orders.Items);

        internal static IReadOnlyDictionary<string, double> Measurements(IEnumerable<ExamOrder> exams)
        {
            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (ExamOrder result in exams.SelectMany(exam => exam.Results)
                .OrderBy(exam => exam.Sequence > 0 ? exam.Sequence : long.MinValue))
                if (result.Measurements != null)
                    foreach (var item in result.Measurements)
                        if (item.Value.HasValue) values[item.Id] = item.Value.Value;
            return values;
        }

        internal static bool Excluded(DiseaseDefinition disease, IReadOnlyDictionary<string, double> values) =>
            disease.Exclusions.Any(rule => rule.Matches(values));

        internal static IEnumerable<(string Title, string Detail)> Summary(ExamOrders orders)
        {
            var values = LatestMeasurements(orders);
            var diseases = DiseaseCatalog.All;
            if (values.Count == 0 || !diseases.Any(disease => disease.Exclusions.Count > 0))
                return Array.Empty<(string, string)>();
            var excluded = diseases.Where(disease => Excluded(disease, values)).Select(disease => disease.Name).ToArray();
            return new[]
            {
                ("Investigação — hipóteses descartadas", excluded.Length == 0 ? "Nenhuma pelos critérios disponíveis." : string.Join(", ", excluded)),
                ("Investigação — demais hipóteses", "Não descartadas pelos critérios disponíveis; isso não confirma diagnóstico.")
            };
        }
    }
}
