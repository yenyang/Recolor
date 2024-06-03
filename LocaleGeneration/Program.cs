using System.Text.Encodings.Web;
using System.Text.Json;

using Colossal;
using Recolor;
using Recolor.Settings;

var setting = new Setting(new Mod());

var locale = new LocaleEN(setting);
var e = new Dictionary<string, string>(
    locale.ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()));
var str = JsonSerializer.Serialize(e, new JsonSerializerOptions()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});

File.WriteAllText("C:\\Users\\TJ\\source\\repos\\Recolor\\Recolor\\UI\\src\\mods\\lang\\en-US.json", str);
