using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Rage;
using Rage.Native;
using static lumen_sante.Gameplay.DoctorProfiles;

namespace lumen_sante.Gameplay
{
    internal static class DoctorAppearance
    {
        private static readonly Dictionary<(uint Model, int Slot, int Piece, int Texture, bool Prop), string> variationNames =
            new Dictionary<(uint, int, int, int, bool), string>();
        private static readonly Dictionary<uint, int[]> hairStyles = new Dictionary<uint, int[]>();
        private static readonly string[] EyeNames =
        {
            null, "Preto", "Azul esverdeado claro", "Azul escuro", "Castanho", "Castanho escuro", "Castanho claro",
            "Azul", "Azul claro", "Rosa", "Amarelo", "Roxo", "Preto intenso", "Verde escuro", "Castanho suave",
            "Amarelo e preto", "Espiral clara", "Vermelho brilhante", "Azul e vermelho", "Preto e azul claro",
            "Branco com borda vermelha", "Serpente verde", "Serpente vermelha", "Serpente azul", "Amarelo escuro",
            "Amarelo brilhante", "Totalmente preto", null, "Pupila vermelha pequena", "Azul e preto",
            "Pupila branca pequena", "Opaco"
        };

        internal static Model ModelFor(Profile profile) =>
            new Model(profile.Female ? "mp_f_freemode_01" : "mp_m_freemode_01");

        internal static Ped ChangeModel(Model model)
        {
            Ped player = Game.LocalPlayer.Character;
            if (player == null || !player.Exists() || !player.IsAlive || !player.IsOnFoot) return null;
            if (player.Model.Hash == model.Hash) return player;
            if (!model.IsValid || !model.IsPed) return null;

            var timeout = Stopwatch.StartNew();
            try
            {
                NativeFunction.Natives.REQUEST_MODEL(model.Hash);
                while (!NativeFunction.Natives.HAS_MODEL_LOADED<bool>(model.Hash))
                {
                    if (timeout.ElapsedMilliseconds >= 3000) return null;
                    GameFiber.Yield();
                }
                if (Game.LocalPlayer.Character != player || !player.Exists() || !player.IsAlive || !player.IsOnFoot)
                    return null;

                int health = player.Health;
                int armor = NativeFunction.Natives.GET_PED_ARMOUR<int>(player);
                NativeFunction.Natives.SET_PLAYER_MODEL(NativeFunction.Natives.PLAYER_ID<int>(), model.Hash);
                Ped replacement = Game.LocalPlayer.Character;
                if (replacement == null || !replacement.Exists()) return null;
                if (replacement.Model.Hash != model.Hash) return null;
                replacement.Health = Math.Min(health, replacement.MaxHealth);
                NativeFunction.Natives.SET_PED_ARMOUR(replacement, armor);
                return replacement;
            }
            finally
            {
                NativeFunction.Natives.SET_MODEL_AS_NO_LONGER_NEEDED(model.Hash);
            }
        }

        internal static void Apply(Ped player, Appearance face)
        {
            SetPiece(player, 0, face.Head, face.HeadTexture, face.HeadPalette);
            NativeFunction.CallByHash<uint>(0x9414E18B9434C2FEUL, player,
                face.Shape[0], face.Shape[1], face.Shape[2], face.Skin[0], face.Skin[1], face.Skin[2],
                Unit(face.Mix[0]), Unit(face.Mix[1]), Unit(face.Mix[2]), false);
            ApplyDetails(player, face);
        }

        internal static void ApplyDetails(Ped player, Appearance face)
        {
            for (int i = 0; i < face.Features.Length; i++)
                SetFaceFeature(player, i, face.Features[i]);

            for (int i = 0; i < face.Overlays.Length; i++) ApplyOverlay(player, face, i);
            ApplyHair(player, face);
            SetEyeColor(player, face.Eyes);
            ApplyTattoos(player, face);
        }

        internal static void SetFaceFeature(Ped player, int index, int value) =>
            NativeFunction.CallByHash<uint>(0x71A5C1DBA060049EUL, player, index,
                Math.Max(-1f, Math.Min(1f, value / 100f)));

        internal static void SetEyeColor(Ped player, int value) =>
            NativeFunction.CallByHash<uint>(0x50B56988B170AFDFUL, player, Math.Max(0, Math.Min(31, value)));

        internal static string EyeColorName(int id) =>
            id >= 0 && id < EyeNames.Length ? EyeNames[id] ?? id.ToString() : id.ToString();

        internal static int[] HairStyles(Ped player)
        {
            uint model = player.Model.Hash;
            if (hairStyles.TryGetValue(model, out int[] styles)) return styles;
            int count = Math.Max(0, Pieces(player, 2, false));
            styles = Enumerable.Range(0, count)
                .Where(id => NativeFunction.CallByHash<bool>(0xE825F6B6CEA7671DUL, player, 2, id, 0))
                .ToArray();
            if (styles.Length > 0) hairStyles[model] = styles;
            return styles;
        }

        internal static string VariationName(Ped player, int slot, int piece, int texture, bool prop)
        {
            if (player == null || !player.Exists() || piece < 0) return null;
            var key = (player.Model.Hash, slot, piece, texture, prop);
            if (variationNames.TryGetValue(key, out string name)) return name;

            uint hash = NativeFunction.CallByHash<uint>(prop ? 0x5D6160275CAEC8DDUL : 0x0368B3A838070348UL,
                player, slot, piece, texture);
            if (hash != 0)
            {
                const int labelOffset = 72;
                const int labelLength = 64;
                var zeros = new byte[labelOffset + labelLength];
                IntPtr buffer = Marshal.AllocHGlobal(zeros.Length);
                try
                {
                    Marshal.Copy(zeros, 0, buffer, zeros.Length);
                    NativeFunction.CallByHash<uint>(prop ? 0x5D5CAFF661DDF6FCUL : 0x74C0E2A57EC66760UL,
                        hash, buffer.ToInt64());
                    name = TranslateLabel(Marshal.PtrToStringAnsi(IntPtr.Add(buffer, labelOffset), labelLength).Split('\0')[0]);
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            variationNames[key] = name;
            return name;
        }

        private static string TranslateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label == "NULL" || label == "NO_LABEL") return null;
            string translation = Marshal.PtrToStringAnsi(NativeFunction.CallByHash<IntPtr>(0x7B5280EBA9840C72UL, label));
            return string.IsNullOrWhiteSpace(translation) || translation == "NULL" || translation == label ? null : translation;
        }

        internal static void ApplyOverlay(Ped player, Appearance face, int index)
        {
            int overlay = face.Overlays[index];
            if (overlay < 0 || overlay >= OverlayCount(index)) overlay = 255;
            NativeFunction.CallByHash<uint>(0x48F44967FA05CC1EUL, player, index, overlay, Unit(face.Opacity[index]));
            int colorType = OverlayColorType(index);
            if (colorType != 0 && overlay != 255)
            {
                int colorCount = colorType == 1 ? HairColors : MakeupColors;
                NativeFunction.CallByHash<uint>(0x497BF74A7B9CB952UL, player, index, colorType,
                    Variation(face.Color[index], colorCount), Variation(face.SecondaryColor[index], colorCount));
            }
        }

        internal static int OverlayColorType(int slot)
        {
            if (slot == 1 || slot == 2 || slot == 4 || slot == 10) return 1;
            return slot == 5 || slot == 8 ? 2 : 0;
        }

        internal static void ApplyHair(Ped player, Appearance face)
        {
            SetPiece(player, 2, face.Hair, face.HairTexture, face.HairPalette);
            int colors = HairColors;
            NativeFunction.CallByHash<uint>(0x4CFFC65454C93A49UL, player,
                Variation(face.HairColor, colors), Variation(face.Highlights, colors));
        }

        internal static int HairColors => NativeFunction.CallByHash<int>(0xE5C0CF872C2AD150UL);
        internal static int MakeupColors => NativeFunction.CallByHash<int>(0xD1F7CA1535D22818UL);
        internal static int OverlayCount(int slot) => NativeFunction.CallByHash<int>(0xCF1CE768BB43480EUL, slot);

        internal static int[] ParentIds()
        {
            var ids = new List<int>();
            for (int group = 0; group < 4; group++)
            {
                int first = NativeFunction.CallByHash<int>(0x68D353AB88B97E0CUL, group);
                int count = NativeFunction.CallByHash<int>(0x5EF37013A6539C9DUL, group);
                if (first >= 0 && count > 0 && count <= 100)
                    ids.AddRange(Enumerable.Range(first, count));
            }
            return ids.Count > 0 ? ids.Distinct().ToArray() : Enumerable.Range(0, 46).ToArray();
        }

        internal sealed class TattooOption
        {
            internal string Name;
            internal Decoration Tattoo;
            internal int Zone;
        }

        private static readonly Dictionary<bool, TattooOption[]> tattooCatalog = new Dictionary<bool, TattooOption[]>();

        internal static TattooOption[] Tattoos(bool female)
        {
            if (tattooCatalog.TryGetValue(female, out TattooOption[] cached)) return cached;
            int faction = female ? 4 : 3;
            int count = NativeFunction.CallByHash<int>(0x278F76C3B0A8F109UL, faction);
            var result = new List<TattooOption>();
            var seen = new HashSet<(uint Collection, uint Preset)>();
            const int bufferLength = 192;
            var zeros = new byte[bufferLength];
            IntPtr buffer = Marshal.AllocHGlobal(bufferLength);
            try
            {
                for (int i = 0; i < Math.Min(count, 20000); i++)
                {
                    if (i > 0 && i % 32 == 0) GameFiber.Yield();
                    Marshal.Copy(zeros, 0, buffer, bufferLength);
                    if (!NativeFunction.CallByHash<bool>(0xFF56381874F82086UL, faction, i, buffer.ToInt64())) continue;
                    // The game's native writes the collection and preset hashes in successive 8-byte slots.
                    uint collection = unchecked((uint)Marshal.ReadInt32(buffer, 16));
                    uint preset = unchecked((uint)Marshal.ReadInt32(buffer, 24));
                    if (collection == 0 || preset == 0 || !seen.Add((collection, preset))) continue;
                    string label = Marshal.PtrToStringAnsi(IntPtr.Add(buffer, 56), 64).Split('\0')[0];
                    string translation = TranslateLabel(label);
                    int zone = NativeFunction.CallByHash<int>(0x9FD452BFBE7A7A8BUL, collection, preset);
                    result.Add(new TattooOption
                    {
                        Name = translation ?? "Tatuagem " + (i + 1),
                        Tattoo = new Decoration { Collection = collection, Preset = preset },
                        Zone = zone >= 0 && zone < 6 ? zone : 6
                    });
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
            tattooCatalog[female] = result.ToArray();
            return tattooCatalog[female];
        }

        internal static void ApplyTattoos(Ped player, Appearance face)
        {
            NativeFunction.Natives.CLEAR_PED_DECORATIONS(player);
            foreach (Decoration tattoo in face.Tattoos)
                NativeFunction.CallByHash<uint>(0x5F5D1665E352A839UL, player, tattoo.Collection, tattoo.Preset);
        }

        internal static Outfit CaptureOutfit(Ped player)
        {
            var outfit = new Outfit();
            for (int slot = 0; slot < 12; slot++)
            {
                outfit.Pieces[slot] = NativeFunction.Natives.GET_PED_DRAWABLE_VARIATION<int>(player, slot);
                outfit.Textures[slot] = NativeFunction.Natives.GET_PED_TEXTURE_VARIATION<int>(player, slot);
                outfit.Palettes[slot] = NativeFunction.Natives.GET_PED_PALETTE_VARIATION<int>(player, slot);
            }
            for (int slot = 0; slot < outfit.Props.Length; slot++)
            {
                outfit.Props[slot] = NativeFunction.Natives.GET_PED_PROP_INDEX<int>(player, slot);
                outfit.PropTextures[slot] = NativeFunction.Natives.GET_PED_PROP_TEXTURE_INDEX<int>(player, slot);
            }
            return outfit;
        }

        internal static bool SameClothing(Outfit first, Outfit second)
        {
            for (int slot = 1; slot < first.Pieces.Length; slot++)
            {
                if (slot == 2) continue;
                if (first.Pieces[slot] != second.Pieces[slot] || first.Textures[slot] != second.Textures[slot] ||
                    first.Palettes[slot] != second.Palettes[slot]) return false;
            }
            for (int slot = 0; slot < first.Props.Length; slot++)
            {
                if (first.Props[slot] != second.Props[slot] ||
                    (first.Props[slot] >= 0 && first.PropTextures[slot] != second.PropTextures[slot])) return false;
            }
            return true;
        }

        internal static void Wear(Ped player, Outfit outfit, bool includeFace = false)
        {
            for (int slot = 0; slot < 12; slot++)
            {
                if (!includeFace && (slot == 0 || slot == 2)) continue;
                SetPiece(player, slot, outfit.Pieces[slot], outfit.Textures[slot], outfit.Palettes[slot]);
            }
            for (int slot = 0; slot < outfit.Props.Length; slot++)
                SetProp(player, slot, outfit.Props[slot], outfit.PropTextures[slot]);
        }

        internal static void SetPiece(Ped player, int slot, int piece, int texture, int palette)
        {
            int count = Pieces(player, slot, false);
            if (count <= 0) return;
            piece = Variation(piece, count);
            texture = Variation(texture, Textures(player, slot, piece, false));
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, slot, piece, texture, Math.Max(0, Math.Min(3, palette)));
        }

        internal static void SetProp(Ped player, int slot, int piece, int texture)
        {
            if (piece < 0 || piece >= Pieces(player, slot, true))
            {
                NativeFunction.Natives.CLEAR_PED_PROP(player, slot);
                return;
            }
            texture = Variation(texture, Textures(player, slot, piece, true));
            NativeFunction.Natives.SET_PED_PROP_INDEX(player, slot, piece, texture, true);
        }

        internal static int Pieces(Ped player, int slot, bool prop) => prop
            ? NativeFunction.Natives.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS<int>(player, slot)
            : NativeFunction.Natives.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS<int>(player, slot);

        internal static int Textures(Ped player, int slot, int piece, bool prop)
        {
            if (piece < 0) return 0;
            return prop ? NativeFunction.Natives.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS<int>(player, slot, piece)
                : NativeFunction.Natives.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS<int>(player, slot, piece);
        }

        private static int Variation(int value, int count) => value >= 0 && value < count ? value : 0;
        private static float Unit(int percentage) => Math.Max(0, Math.Min(100, percentage)) / 100f;
    }
}
