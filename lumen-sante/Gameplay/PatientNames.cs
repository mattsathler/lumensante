using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

namespace lumen_sante.Gameplay
{
    internal static class PatientNames
    {
        private static readonly string[] Male = Load("nomes-masculinos.json", 200);
        private static readonly string[] Female = Load("nomes-femininos.json", 200);
        private static readonly string[] Surnames = Load("sobrenomes.json", 400);

        internal static string Pick(Random random, bool female)
        {
            string[] names = female ? Female : Male;
            return names[random.Next(names.Length)] + " " + Surnames[random.Next(Surnames.Length)];
        }

        private static string[] Load(string file, int minimum)
        {
            string resource = "lumen_sante.Data." + file;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    throw new InvalidDataException("Lista de nomes ausente: " + resource);
                }

                using (var reader = new StreamReader(stream))
                {
                    var names = new JavaScriptSerializer().Deserialize<string[]>(reader.ReadToEnd());
                    if (names == null || names.Length < minimum || names.Any(string.IsNullOrWhiteSpace) ||
                        names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
                    {
                        throw new InvalidDataException("Lista de nomes inválida: " + resource);
                    }

                    return names;
                }
            }
        }
    }
}
