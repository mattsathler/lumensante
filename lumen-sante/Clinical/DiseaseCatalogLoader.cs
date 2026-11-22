using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace lumen_sante.Clinical
{
    internal static class DiseaseCatalogLoader
    {
        internal static DiseaseDefinition[] Load(string directory, Action<string> log)
        {
            var assembly = typeof(DiseaseCatalogLoader).Assembly;
            var official = new List<DiseaseDefinition>();
            foreach (string resource in assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("lumen_sante.Data.Doencas.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal))
            {
                TryRead(resource, () =>
                {
                    using (var reader = new StreamReader(assembly.GetManifestResourceStream(resource), Encoding.UTF8, true))
                        return reader.ReadToEnd();
                }, definition => official.Add(definition), log);
            }

            var officialIds = new HashSet<string>(official.Select(disease => disease.Id), StringComparer.OrdinalIgnoreCase);
            if (officialIds.Count != official.Count) throw new InvalidDataException("IDs duplicados nas doenças oficiais.");
            var external = new List<(string File, DiseaseDefinition Disease)>();
            string[] files = new string[0];
            try
            {
                if (Directory.Exists(directory)) files = Directory.GetFiles(directory, "*.json");
            }
            catch (IOException error) { log(directory + ": " + error.Message); }
            catch (UnauthorizedAccessException error) { log(directory + ": " + error.Message); }
            catch (SecurityException error) { log(directory + ": " + error.Message); }
            foreach (string file in files.OrderBy(name => name, StringComparer.Ordinal))
            {
                TryRead(file, () => File.ReadAllText(file, Encoding.UTF8), definition =>
                {
                    if (officialIds.Contains(definition.Id)) log(file + ": $.Id reservado pelo catálogo oficial: " + definition.Id);
                    else external.Add((file, definition));
                }, log);
            }

            var result = new List<DiseaseDefinition>(official);
            long total = result.Sum(disease => (long)disease.SelectionWeight);
            foreach (var group in external.GroupBy(entry => entry.Disease.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() > 1)
                {
                    log("$.Id duplicado " + group.Key + ": " + string.Join(", ", group.Select(candidate => candidate.File)));
                    continue;
                }
                var entry = group.Single();
                if (total + entry.Disease.SelectionWeight > int.MaxValue)
                {
                    log(entry.File + ": $.SelectionWeight excede a soma máxima do catálogo.");
                    continue;
                }
                result.Add(entry.Disease);
                total += entry.Disease.SelectionWeight;
                log(entry.File + ": doença externa carregada: " + entry.Disease.Id);
            }
            if (result.Count == 0 || total > int.MaxValue)
                throw new InvalidDataException("Catálogo de doenças vazio ou soma de pesos inválida.");
            return result.ToArray();
        }

        private static void TryRead(string source, Func<string> read, Action<DiseaseDefinition> accept, Action<string> log)
        {
            try { accept(DiseaseJsonReader.Read(read())); }
            catch (ArgumentException error) { log(source + ": " + error.Message); }
            catch (InvalidOperationException error) { log(source + ": JSON inválido: " + error.Message); }
            catch (FormatException error) { log(source + ": JSON inválido: " + error.Message); }
            catch (OverflowException error) { log(source + ": número fora da faixa: " + error.Message); }
            catch (IOException error) { log(source + ": " + error.Message); }
            catch (UnauthorizedAccessException error) { log(source + ": " + error.Message); }
            catch (SecurityException error) { log(source + ": " + error.Message); }
        }
    }
}
