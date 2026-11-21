using System.Linq;

namespace lumen_sante.Clinical
{
    internal static class PatientDialogue
    {
        private static readonly string[] Openings =
        {
            "", "Olha, ", "Bom, ", "Então, ", "Pra ser sincero, "
        };

        internal static string[] Vary(params string[] statements)
        {
            return statements.SelectMany(statement => Openings.Select(opening =>
                opening.Length == 0 ? statement : opening + char.ToLowerInvariant(statement[0]) +
                    statement.Substring(1))).ToArray();
        }
    }
}
