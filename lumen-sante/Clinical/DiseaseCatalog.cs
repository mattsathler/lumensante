using System;
using System.Linq;
using System.IO;

namespace lumen_sante.Clinical
{
    internal static class DiseaseCatalog
    {
        private static readonly System.Collections.Generic.List<string> Messages = new System.Collections.Generic.List<string>();
        private static readonly DiseaseDefinition[] Cases = DiseaseCatalogLoader.Load(
            Path.Combine(PluginPaths.InstallationDirectory, "LumenSante", "Diseases"),
            Messages.Add);
        internal static System.Collections.Generic.IReadOnlyList<string> LoadDiagnostics { get; } = Messages.AsReadOnly();
        private static readonly System.Collections.Generic.Dictionary<string, DiseaseDefinition> ById =
            Cases.ToDictionary(disease => disease.Id, StringComparer.Ordinal);
        internal static DiseaseDefinition Find(string id)
        {
            DiseaseDefinition disease;
            return id != null && ById.TryGetValue(id, out disease) ? disease : null;
        }
        internal static System.Collections.Generic.IReadOnlyList<DiseaseDefinition> Treatments { get; } =
            Array.AsReadOnly(Cases.Where(disease => disease.RequiresSpecificTreatment).ToArray());
        private static readonly int TotalWeight = Cases.Sum(disease => disease.SelectionWeight);
        internal static int Count => Cases.Length;
        internal static System.Collections.Generic.IReadOnlyList<DiseaseDefinition> All { get; } = Array.AsReadOnly(Cases);

        internal static DiseaseDefinition Pick(Random random)
        {
            if (Cases.Length == 0) throw new InvalidOperationException("Nenhuma doença disponível na pasta LumenSante/Diseases.");
            int roll = random.Next(TotalWeight);
            foreach (DiseaseDefinition disease in Cases)
            {
                if (roll < disease.SelectionWeight)
                {
                    return disease;
                }

                roll -= disease.SelectionWeight;
            }

            throw new InvalidOperationException("Catálogo de doenças inválido.");
        }
    }
}
