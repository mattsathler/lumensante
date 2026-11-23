using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace lumen_sante.Gameplay
{
    internal sealed class DoctorProfiles
    {
        internal const int OutfitLimit = 20;
        public sealed class Profile
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; }
            public bool Female { get; set; }
            public Appearance Face { get; set; } = new Appearance();
            public List<Outfit> Outfits { get; set; } = new List<Outfit>();
            public Guid LastCasual { get; set; }
        }

        public sealed class Appearance
        {
            public int[] Shape { get; set; } = new[] { 0, 21, 0 };
            public int[] Skin { get; set; } = new[] { 0, 21, 0 };
            public int[] Mix { get; set; } = new[] { 50, 50, 0 };
            public int[] Features { get; set; } = new int[20];
            public int[] Overlays { get; set; } = Enumerable.Repeat(-1, 13).ToArray();
            public int[] Opacity { get; set; } = Enumerable.Repeat(100, 13).ToArray();
            public int[] Color { get; set; } = new int[13];
            public int[] SecondaryColor { get; set; } = new int[13];
            public int Head { get; set; }
            public int HeadTexture { get; set; }
            public int HeadPalette { get; set; }
            public int Hair { get; set; }
            public int HairTexture { get; set; }
            public int HairPalette { get; set; }
            public int HairColor { get; set; }
            public int Highlights { get; set; }
            public int Eyes { get; set; }
            public List<Decoration> Tattoos { get; set; } = new List<Decoration>();
        }

        public sealed class Decoration
        {
            public uint Collection { get; set; }
            public uint Preset { get; set; }
        }

        public sealed class Outfit
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; }
            public bool Duty { get; set; }
            public int[] Pieces { get; set; } = new int[12];
            public int[] Textures { get; set; } = new int[12];
            public int[] Palettes { get; set; } = new int[12];
            public int[] Props { get; set; } = Enumerable.Repeat(-1, 13).ToArray();
            public int[] PropTextures { get; set; } = new int[13];
        }

        public sealed class Data
        {
            public int Version { get; set; } = 1;
            public Guid DefaultId { get; set; }
            public List<Profile> Profiles { get; set; } = new List<Profile>();
        }

        private readonly string root;
        private readonly string path;
        private readonly Action<string> log;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
        private Data data = new Data();
        internal bool Available { get; private set; } = true;
        internal IReadOnlyList<Profile> Items => data.Profiles;
        internal Guid DefaultId => data.DefaultId;
        internal Profile Default => Items.FirstOrDefault(profile => profile.Id == DefaultId);

        internal DoctorProfiles(string root, Action<string> log)
        {
            this.root = root;
            this.log = log;
            path = Path.Combine(root, "doctors.json");
            try
            {
                if (!File.Exists(path)) return;
                Data loaded = json.Deserialize<Data>(File.ReadAllText(path, Encoding.UTF8));
                if (!Valid(loaded)) throw new InvalidDataException("Formato dos perfis inválido.");
                data = loaded;
            }
            catch (Exception error) when (StorageError(error))
            {
                Available = false;
                log("Perfis médicos indisponíveis: " + error.Message);
            }
        }

        internal string AgendaPath(Guid id) => Path.Combine(root, "Agendas", id.ToString("N"), "agenda.json");
        internal T Copy<T>(T value) => json.Deserialize<T>(json.Serialize(value));
        internal string EditStamp<T>(T value) => json.Serialize(value);

        internal static Profile Create(string name, bool female)
        {
            var profile = new Profile { Name = name, Female = female };
            profile.Face.Shape[0] = profile.Face.Skin[0] = female ? 21 : 0;
            profile.Face.Shape[1] = profile.Face.Skin[1] = female ? 22 : 1;
            var casual = new Outfit { Name = "Roupa casual" };
            var scrub = new Outfit { Name = "Scrub", Duty = true };
            if (female)
            {
                scrub.Pieces = new[] { 0, 0, 0, 0, 3, 0, 119, 14, 0, 0, 0, 194 };
                scrub.Textures[6] = 2;
                scrub.Palettes[6] = 1;
                casual.Pieces = new[] { 0, 0, 0, 15, 0, 0, 3, 0, 15, 0, 0, 0 };
            }
            else
            {
                casual.Pieces[4] = 1;
                casual.Pieces[6] = 1;
                casual.Pieces[8] = 15;
                scrub.Pieces[4] = 5;
                scrub.Pieces[6] = 1;
                scrub.Pieces[8] = 15;
                scrub.Pieces[11] = 2;
                scrub.Textures[4] = 2;
                scrub.Textures[11] = 2;
            }
            profile.Outfits.Add(scrub);
            profile.Outfits.Add(casual);
            profile.LastCasual = casual.Id;
            return profile;
        }

        internal bool Put(Profile profile) => SaveProfile(profile, null);

        internal bool SaveProfile(Profile profile, bool? makeDefault)
        {
            if (!ValidProfile(profile)) return false;
            Data next = Copy(data);
            int index = next.Profiles.FindIndex(item => item.Id == profile.Id);
            Profile saved = Copy(profile);
            if (index < 0) next.Profiles.Add(saved);
            else next.Profiles[index] = saved;
            if (makeDefault == true) next.DefaultId = profile.Id;
            else if (makeDefault == false && next.DefaultId == profile.Id) next.DefaultId = Guid.Empty;
            return Commit(next);
        }

        internal bool Remove(Guid id)
        {
            Data next = Copy(data);
            if (next.Profiles.RemoveAll(profile => profile.Id == id) == 0) return false;
            if (next.DefaultId == id) next.DefaultId = Guid.Empty;
            // Leave the doctor's agenda under its original ID.
            return Commit(next);
        }

        internal bool RememberCasual(Guid id, Guid outfit)
        {
            Profile profile = Items.FirstOrDefault(item => item.Id == id);
            if (profile == null || !profile.Outfits.Any(item => item.Id == outfit && !item.Duty)) return false;
            if (profile.LastCasual == outfit) return true;
            Profile edited = Copy(profile);
            edited.LastCasual = outfit;
            return Put(edited);
        }

        private bool Commit(Data next)
        {
            if (!Available || !Valid(next)) return false;
            try
            {
                Directory.CreateDirectory(root);
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, json.Serialize(next), Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                data = next;
                return true;
            }
            catch (Exception error) when (StorageError(error))
            {
                log("Falha ao salvar médicos: " + error.Message);
                return false;
            }
        }

        private static bool StorageError(Exception error) => error is IOException || error is InvalidDataException ||
            error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException ||
            error is FormatException;

        private static bool Valid(Data value) => value?.Version == 1 && value.Profiles != null &&
            value.Profiles.All(ValidProfile) && value.Profiles.Select(profile => profile.Id).Distinct().Count() == value.Profiles.Count &&
            (value.DefaultId == Guid.Empty || value.Profiles.Any(profile => profile.Id == value.DefaultId));

        private static bool ValidProfile(Profile profile)
        {
            Appearance face = profile?.Face;
            return face != null && profile.Id != Guid.Empty && !string.IsNullOrWhiteSpace(profile.Name) &&
                Sized(face.Shape, 3) && Sized(face.Skin, 3) && Sized(face.Mix, 3) && Sized(face.Features, 20) &&
                Sized(face.Overlays, 13) && Sized(face.Opacity, 13) && Sized(face.Color, 13) &&
                Sized(face.SecondaryColor, 13) && face.Tattoos != null && face.Tattoos.All(item => item != null) &&
                profile.Outfits != null && profile.Outfits.Count <= OutfitLimit &&
                profile.Outfits.All(item => item != null && item.Id != Guid.Empty && !string.IsNullOrWhiteSpace(item.Name) &&
                    Sized(item.Pieces, 12) && Sized(item.Textures, 12) && Sized(item.Palettes, 12) &&
                    Sized(item.Props, 13) && Sized(item.PropTextures, 13)) &&
                profile.Outfits.Select(item => item.Id).Distinct().Count() == profile.Outfits.Count;
        }

        private static bool Sized(int[] values, int size) => values != null && values.Length == size;
    }
}
