using System;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal static class DiseaseCatalog
    {
        private static readonly DiseaseDefinition[] Cases = { LungCancerCase.Definition };
        private static readonly int TotalWeight = Cases.Sum(disease => disease.SelectionWeight);

        internal static DiseaseDefinition Pick(Random random)
        {
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
