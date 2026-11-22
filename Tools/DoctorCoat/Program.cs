
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

if (args.Length != 2)
{
    throw new ArgumentException("Usage: DoctorCoat <Doctor_Coat source directory> <output.oiv>");
}

const string drawableFile = "jbib_000_u.ydd";
const string textureFile = "jbib_diff_000_a_uni.ytd";

var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (!Path.GetExtension(output).Equals(".oiv", StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("The output file must have the .oiv extension.");
}

var coats = new[] { (Gender: "male", Ped: "mp_m_freemode_01", Character: "SCR_CHAR_MULTIPLAYER"),
                    (Gender: "female", Ped: "mp_f_freemode_01", Character: "SCR_CHAR_MULTIPLAYER_F") }
    .Select(item => new
    {
        item.Gender,
        item.Ped,
        item.Character,
        Drawable = File.ReadAllBytes(Path.Combine(source, item.Gender, drawableFile)),
        Texture = File.ReadAllBytes(Path.Combine(source, item.Gender, textureFile))
    }).ToArray();

var temporary = Directory.CreateTempSubdirectory("lumen-doctor-coat-");
try
{
    var archive = new XElement("archive",
        new XAttribute("path", @"update\x64\dlcpacks\mpimportexport\dlc.rpf"),
        new XAttribute("createIfNotExist", "False"), new XAttribute("type", "RPF7"));
    foreach (var coat in coats)
    {
        var gender = coat.Gender == "male" ? "m" : "f";
        var collection = coat.Ped + "_mp_" + gender + "_importexport_01";
        var inner = new XElement("archive",
            new XAttribute("path", @"x64\models\cdimages\mpimportexport_" + coat.Gender + ".rpf"),
            new XAttribute("createIfNotExist", "False"), new XAttribute("type", "RPF7"),
            new XElement("add", new XAttribute("source", coat.Gender + "/" + drawableFile),
                collection + @"\jbib_006_u.ydd"),
            new XElement("add", new XAttribute("source", coat.Gender + "/" + textureFile),
                collection + @"\jbib_diff_006_a_uni.ytd"));
        archive.Add(inner);
    }
    var manifest = new XElement("package",
        new XAttribute("version", "2.2"),
        new XAttribute("id", "{B0B0BF4A-7918-4BDA-9098-33E6BE9C63A5}"),
        new XAttribute("target", "Five"),
        new XElement("metadata",
            new XElement("name", "Lumen Santé - Doctor Coat (Import Export)"),
            new XElement("version", new XElement("major", 2), new XElement("minor", 0)),
            new XElement("author", new XElement("displayName", "Lumen Santé (pacote)")),
            new XElement("description", new XCData(
                "Substitui o casaco Import/Export jbib_006 e sua textura A para MP Male e MP Female. Instale na pasta mods."))),
        new XElement("colors",
            new XElement("headerBackground", new XAttribute("useBlackTextColor", "False"), "$FF23366A"),
            new XElement("iconBackground", "$FF3B5998")),
        new XElement("content", archive));
    var staged = Path.Combine(temporary.FullName, "package.oiv");
    using (var zip = ZipFile.Open(staged, ZipArchiveMode.Create))
    {
        using (var writer = new StreamWriter(zip.CreateEntry("assembly.xml").Open(), new UTF8Encoding(false)))
        {
            writer.Write(XmlText(manifest));
        }
        foreach (var coat in coats)
        {
            zip.CreateEntryFromFile(Path.Combine(source, coat.Gender, drawableFile),
                "content/" + coat.Gender + "/" + drawableFile);
            zip.CreateEntryFromFile(Path.Combine(source, coat.Gender, textureFile),
                "content/" + coat.Gender + "/" + textureFile);
        }
    }

    File.Move(staged, output, overwrite: false);
    Console.WriteLine(output);
}
finally
{
    temporary.Delete(recursive: true);
}

static string XmlText(XElement xml) => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xml + "\n";


