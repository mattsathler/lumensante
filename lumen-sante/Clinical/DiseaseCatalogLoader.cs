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
            var entries = new List<(string File, DiseaseDefinition Disease)>();
            string[] files = new string[0];
            try
            {
                if (Directory.Exists(directory)) files = Directory.GetFiles(directory, "*.json");
                else log(directory + ": pasta de doenças ausente.");
            }
            catch (IOException error) { log(directory + ": " + error.Message); }
            catch (UnauthorizedAccessException error) { log(directory + ": " + error.Message); }
            catch (SecurityException error) { log(directory + ": " + error.Message); }
            foreach (string file in files.OrderBy(name => name, StringComparer.Ordinal))
            {
                TryRead(file, () => File.ReadAllText(file, Encoding.UTF8), definition =>
                {
                    entries.Add((file, definition));
                }, log);
            }

            var result = new List<DiseaseDefinition>();
            long total = 0;
            foreach (var group in entries.GroupBy(entry => entry.Disease.Id, StringComparer.OrdinalIgnoreCase))
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
                log(entry.File + ": doença carregada: " + entry.Disease.Id);
            }
            if (result.Count == 0) log(directory + ": nenhum arquivo de doença válido encontrado.");
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
