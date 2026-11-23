using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal static class PatientAppearance
    {
        private static readonly List<Model> remaining = new List<Model>();
        private static readonly HashSet<Model> rejected = new HashSet<Model>();
        private static Model[] catalog;
        private static readonly string[] CityModels =
        {
            "a_f_m_beach_01", "a_f_m_bevhills_01", "a_f_m_bevhills_02", "a_f_m_business_02",
            "a_f_m_downtown_01", "a_f_m_eastsa_01", "a_f_m_eastsa_02", "a_f_m_fatbla_01",
            "a_f_m_fatwhite_01", "a_f_m_ktown_01", "a_f_m_ktown_02", "a_f_m_skidrow_01",
            "a_f_o_genstreet_01", "a_f_o_soucent_01", "a_f_o_soucent_02",
            "a_f_y_beach_01", "a_f_y_bevhills_01", "a_f_y_bevhills_02", "a_f_y_bevhills_03",
            "a_f_y_business_01", "a_f_y_business_02", "a_f_y_business_03", "a_f_y_business_04",
            "a_f_y_eastsa_01", "a_f_y_eastsa_02", "a_f_y_eastsa_03", "a_f_y_fitness_01",
            "a_f_y_genhot_01", "a_f_y_hipster_01", "a_f_y_hipster_02", "a_f_y_hipster_03",
            "a_f_y_hipster_04", "a_f_y_runner_01", "a_f_y_soucent_01", "a_f_y_soucent_02",
            "a_f_y_soucent_03", "a_f_y_tourist_01", "a_f_y_vinewood_01", "a_f_y_vinewood_02",
            "a_m_m_beach_01", "a_m_m_bevhills_01", "a_m_m_bevhills_02", "a_m_m_business_01",
            "a_m_m_eastsa_01", "a_m_m_eastsa_02", "a_m_m_genfat_01", "a_m_m_genfat_02",
            "a_m_m_ktown_01", "a_m_m_malibu_01", "a_m_m_skidrow_01", "a_m_m_soucent_01",
            "a_m_m_soucent_02", "a_m_m_soucent_03", "a_m_m_soucent_04", "a_m_o_beach_01",
            "a_m_o_genstreet_01", "a_m_o_soucent_01", "a_m_o_soucent_02", "a_m_y_beach_01",
            "a_m_y_beach_02", "a_m_y_beach_03", "a_m_y_beachvesp_01", "a_m_y_beachvesp_02",
            "a_m_y_bevhills_01", "a_m_y_bevhills_02", "a_m_y_business_01", "a_m_y_business_02",
            "a_m_y_business_03", "a_m_y_downtown_01", "a_m_y_eastsa_01", "a_m_y_eastsa_02",
            "a_m_y_genstreet_01", "a_m_y_genstreet_02", "a_m_y_hipster_01", "a_m_y_hipster_02",
            "a_m_y_hipster_03", "a_m_y_ktown_01", "a_m_y_ktown_02", "a_m_y_runner_01",
            "a_m_y_soucent_01", "a_m_y_soucent_02", "a_m_y_soucent_03", "a_m_y_soucent_04",
            "a_m_y_stbla_01", "a_m_y_stbla_02", "a_m_y_stlat_01", "a_m_y_stwhi_01",
            "a_m_y_stwhi_02", "a_m_y_vinewood_01", "a_m_y_vinewood_02", "a_m_y_vinewood_03"
        };

        internal sealed class Snapshot
        {
            private readonly int[] drawables = new int[12];
            private readonly int[] textures = new int[12];
            private readonly int[] palettes = new int[12];
            private readonly int[] props = new int[8];
            private readonly int[] propTextures = new int[8];

            internal Snapshot(Ped patient)
            {
                for (int slot = 0; slot < drawables.Length; slot++)
                {
                    drawables[slot] = NativeFunction.Natives.GET_PED_DRAWABLE_VARIATION<int>(patient, slot);
                    textures[slot] = NativeFunction.Natives.GET_PED_TEXTURE_VARIATION<int>(patient, slot);
                    palettes[slot] = NativeFunction.Natives.GET_PED_PALETTE_VARIATION<int>(patient, slot);
                }
                for (int slot = 0; slot < props.Length; slot++)
                {
                    props[slot] = NativeFunction.Natives.GET_PED_PROP_INDEX<int>(patient, slot);
                    propTextures[slot] = NativeFunction.Natives.GET_PED_PROP_TEXTURE_INDEX<int>(patient, slot);
                }
            }

            internal Snapshot(int[] data)
            {
                Array.Copy(data, 0, drawables, 0, 12);
                Array.Copy(data, 12, textures, 0, 12);
                Array.Copy(data, 24, palettes, 0, 12);
                Array.Copy(data, 36, props, 0, 8);
                Array.Copy(data, 44, propTextures, 0, 8);
            }

            internal int[] Capture() => drawables.Concat(textures).Concat(palettes).Concat(props).Concat(propTextures).ToArray();

            internal void Apply(Ped patient)
            {
                for (int slot = 0; slot < drawables.Length; slot++)
                {
                    NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(patient, slot, drawables[slot], textures[slot], palettes[slot]);
                }
                NativeFunction.Natives.CLEAR_ALL_PED_PROPS(patient);
                for (int slot = 0; slot < props.Length; slot++)
                {
                    if (props[slot] >= 0)
                    {
                        NativeFunction.Natives.SET_PED_PROP_INDEX(patient, slot, props[slot], propTextures[slot], true);
                    }
                }
            }
        }

        internal static Model ScheduledModel(bool female) =>
            new Model(female ? "a_f_y_business_01" : "a_m_y_business_01");

        internal static Ped Create(Random random, Vector3 position, float heading)
        {
            if (catalog == null)
            {
                catalog = CityModels.Select(name => new Model(name))
                    .Where(model => model.IsValid && model.IsPed).Distinct().ToArray();
                Game.LogTrivial("[Lumen Santé] Catálogo de pacientes: " + catalog.Length + " modelos.");
            }

            var present = new HashSet<Model>(World.GetAllPeds().Where(ped => ped.Exists()).Select(ped => ped.Model));
            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (remaining.Count == 0)
                {
                    remaining.AddRange(catalog.Where(candidate => !rejected.Contains(candidate)));
                    for (int index = remaining.Count - 1; index > 0; index--)
                    {
                        int other = random.Next(index + 1);
                        Model temporary = remaining[index];
                        remaining[index] = remaining[other];
                        remaining[other] = temporary;
                    }
                }
                if (remaining.Count == 0) break;
                int chosen = remaining.FindIndex(candidate => !present.Contains(candidate));
                if (chosen < 0) chosen = 0;
                Model model = remaining[chosen];
                remaining.RemoveAt(chosen);
                Ped patient = null;
                try
                {
                    patient = new Ped(model, position, heading);
                    if (!patient.Exists() || !patient.IsHuman)
                    {
                        rejected.Add(model);
                        if (patient.Exists()) patient.Delete();
                        continue;
                    }
                    NativeFunction.Natives.SET_PED_RANDOM_COMPONENT_VARIATION(patient, false);
                    NativeFunction.Natives.SET_PED_RANDOM_PROPS(patient);
                    return patient;
                }
                catch (InvalidOperationException error)
                {
                    if (patient != null && patient.Exists()) patient.Delete();
                    rejected.Add(model);
                    Game.LogTrivial("[Lumen Santé] Modelo de paciente indisponível: " + model.Name + ". " + error.Message);
                }
            }
            throw new InvalidOperationException("Não foi possível criar um paciente humano disponível.");
        }
    }
}
