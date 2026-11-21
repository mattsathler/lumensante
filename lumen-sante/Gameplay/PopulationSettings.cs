using System;
using System.IO;
using Rage;

namespace lumen_sante.Gameplay
{
    internal sealed class PopulationSettings
    {
        internal const int MinimumInsideForDeparture = 30;
        internal const int MinimumInitialPopulation = 20;
        private const int DefaultMaxAmbientNpcPatients = 36;
        private const int DefaultMaxWorkers = 24;

        internal int MaxAmbientNpcPatients { get; }
        internal int MaxWorkers { get; }

        private PopulationSettings(int maxAmbientNpcPatients, int maxWorkers)
        {
            MaxAmbientNpcPatients = maxAmbientNpcPatients;
            MaxWorkers = maxWorkers;
        }

        internal static PopulationSettings Load()
        {
            string directory = Path.GetDirectoryName(typeof(PluginEntryPoint).Assembly.Location);
            string file = Path.Combine(directory, "lumen-sante.ini");
            int maxPatients = DefaultMaxAmbientNpcPatients;
            int maxWorkers = DefaultMaxWorkers;
            try
            {
                if (File.Exists(file))
                {
                    foreach (string line in File.ReadLines(file))
                    {
                        string entry = line.Split(';')[0];
                        int separator = entry.IndexOf('=');
                        if (separator < 0 || !int.TryParse(entry.Substring(separator + 1).Trim(), out int value) || value < 0)
                        {
                            continue;
                        }

                        string key = entry.Substring(0, separator).Trim();
                        if (key.Equals(nameof(MaxAmbientNpcPatients), StringComparison.OrdinalIgnoreCase))
                        {
                            maxPatients = value;
                        }
                        else if (key.Equals(nameof(MaxWorkers), StringComparison.OrdinalIgnoreCase))
                        {
                            maxWorkers = value;
                        }
                    }
                }

                if ((long)maxPatients + maxWorkers >= MinimumInitialPopulation)
                {
                    return new PopulationSettings(maxPatients, maxWorkers);
                }

                Game.LogTrivial("[Lumen Santé] A soma dos limites é inferior à população inicial de 20. Usando os limites padrão.");
            }
            catch (IOException error)
            {
                Game.LogTrivial("[Lumen Santé] Falha ao ler os limites de população: " + error.Message);
            }
            catch (UnauthorizedAccessException error)
            {
                Game.LogTrivial("[Lumen Santé] Falha ao ler os limites de população: " + error.Message);
            }

            return new PopulationSettings(DefaultMaxAmbientNpcPatients, DefaultMaxWorkers);
        }
    }
}
