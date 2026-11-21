using System;
using System.Linq;
using Rage;

namespace lumen_sante.Gameplay
{
    internal static class PatientAppearance
    {
        private static readonly string[] FallbackModels =
        {
            "a_f_y_business_01", "a_f_y_hipster_02", "a_f_m_bevhills_01",
            "a_m_y_business_02", "a_m_y_hipster_01", "a_m_m_bevhills_02"
        };

        internal static Model Pick(Random random, Ped player)
        {
            Model[] models = World.GetAllPeds()
                .Where(ped => ped.Exists() && ped != player && ped.IsAlive && ped.IsHuman)
                .Select(ped => ped.Model)
                .Where(model => model.IsLoaded)
                .Distinct()
                .ToArray();

            return models.Length > 0
                ? models[random.Next(models.Length)]
                : new Model(FallbackModels[random.Next(FallbackModels.Length)]);
        }
    }
}
