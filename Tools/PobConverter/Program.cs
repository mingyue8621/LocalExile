using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

// PoB → LocalExile build converter (the pob mod's "external converter", contract Version 1).
//
// stdin : { "Version":1, "Code":"<PoB share code or raw XML>", "Name":"<character>", "Data":"<abs data folder>",
//           "Record": { ... server character record ... }, "SourceFile": null }
// stdout: { "Version":1, "Record": {...}, "Save": {...}, "Summary":"...", "Warnings":[...] }  (exit 0)
// stderr: the reason for a non-zero exit.
//
// What it imports today: class, level and the ordinary passive tree. Equipment, gems, flasks, jewels and the
// ascendancy are NOT imported (they need the item/mod and ascendancy-award model the mod's native save expects);
// each one is reported as a warning so the player knows what stayed as it was.

// The mod writes UTF-8 and reads UTF-8; a console's default codepage would mangle a character name.
using var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
var input = stdin.ReadToEnd();
JsonNode? request;
try { request = JsonNode.Parse(input); }
catch (Exception error) { return Fail("stdin is not JSON: " + error.Message); }
if (request is not JsonObject root) return Fail("stdin is not a JSON object.");
if (root["Version"]?.GetValue<int>() != 1) return Fail("unsupported converter request version.");
string code = root["Code"]?.GetValue<string>() ?? "";
string dataFolder = root["Data"]?.GetValue<string>() ?? "data";
if (root["Record"] is not JsonObject character) return Fail("the request carries no character record.");
if (string.IsNullOrWhiteSpace(code)) return Fail("no build code was given.");

string xml;
try { xml = Decode(code); }
catch (Exception error) { return Fail("the build code could not be decoded: " + error.Message); }

XDocument document;
try { document = XDocument.Parse(xml); }
catch (Exception error) { return Fail("the decoded build is not XML: " + error.Message); }

var build = document.Root?.Element("Build") ?? throw new InvalidDataException("the build has no <Build> element.");
string className = build.Attribute("className")?.Value ?? "";
string ascendancy = build.Attribute("ascendClassName")?.Value ?? "";
int level = int.TryParse(build.Attribute("level")?.Value, out int parsedLevel) ? Math.Clamp(parsedLevel, 1, 100) : 1;

var warnings = new List<string>();
int classIndex = ClassIndex(className);
if (classIndex < 0) return Fail($"unknown class '{className}'.");
string currentClass = ClassName(character["Class"]?.GetValue<int>() ?? -1);
if (currentClass.Length > 0 && currentClass != className)
    warnings.Add($"build class is {className}; the character becomes {className} (was {currentClass}).");

// The active spec's allocated nodes.
var tree = document.Root?.Element("Tree");
int active = int.TryParse(tree?.Attribute("activeSpec")?.Value, out int specIndex) ? specIndex : 1;
var specs = tree?.Elements("Spec").ToList() ?? [];
XElement? spec = specs.Count == 0 ? null : specs[Math.Clamp(active - 1, 0, specs.Count - 1)];
if (spec is null) return Fail("the build has no passive tree spec.");
var nodes = (spec.Attribute("nodes")?.Value ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(text => ushort.TryParse(text, out ushort id) ? id : (ushort)0)
    .Where(id => id != 0)
    .Distinct()
    .ToList();

// Which of them are ordinary nodes: PassiveSkills' AscendancyKey is null for them and set on ascendancy nodes.
var (ordinary, ascendancyNodes, unknown) = Classify(nodes, dataFolder);
if (unknown.Count > 0)
    warnings.Add($"{unknown.Count} node(s) are not in this server's passive table (a build for another game version?): "
        + string.Join(", ", unknown.Take(12)) + (unknown.Count > 12 ? " …" : ""));
if (ascendancyNodes.Count > 0)
    warnings.Add($"{ascendancyNodes.Count} ascendancy node(s) are not imported"
        + (ascendancy.Length > 0 ? $" ({ascendancy}: the points come from the Labyrinth's awards)." : "."));
warnings.Add("Equipment, gems, flasks and jewels are not imported: the build's items stay as they are.");

var save = new JsonObject
{
    ["Inventories"] = new JsonObject(),
    ["Passives"] = new JsonArray(ordinary.Select(id => (JsonNode)id).ToArray()),
};
character["Class"] = classIndex;
character["Level"] = level;
var result = new JsonObject
{
    ["Version"] = 1,
    ["Record"] = character.DeepClone(),
    ["Save"] = save,
    ["Summary"] = $"{className}{Localised(className)} 等级 {level}: {ordinary.Count} 个天赋点已导入"
        + (ascendancyNodes.Count > 0 ? $"，另有 {ascendancyNodes.Count} 个升华点未导入" : "")
        + "；装备与宝石保持原样。",
    ["Warnings"] = new JsonArray(warnings.Select(text => (JsonNode)text).ToArray()),
};
using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
stdout.Write(result.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
return 0;

// A share code is base64 (URL-safe, padding optional) over zlib-deflated XML; older builds pasted the raw XML.
static string Decode(string code)
{
    string trimmed = code.Trim();
    if (trimmed.StartsWith('<')) return trimmed;
    string normalised = trimmed.Replace('-', '+').Replace('_', '/').TrimEnd('=');
    int padding = normalised.Length % 4;
    if (padding > 0) normalised += new string('=', 4 - padding);
    byte[] compressed = Convert.FromBase64String(normalised);
    try
    {
        using var zlib = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var reader = new StreamReader(zlib, Encoding.UTF8);
        string text = reader.ReadToEnd();
        if (text.Contains('<')) return text;
    }
    catch (InvalidDataException) { }
    using var raw = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
    using var fallback = new StreamReader(raw, Encoding.UTF8);
    return fallback.ReadToEnd();
}

// The ordinary nodes, the ascendancy ones and the ids the server's passive table does not know.
static (List<ushort> Ordinary, List<ushort> Ascendancy, List<ushort> Unknown) Classify(List<ushort> nodes, string dataFolder)
{
    var ordinary = new List<ushort>();
    var ascendancy = new List<ushort>();
    var unknown = new List<ushort>();
    var ascendancyIds = new HashSet<ushort>();
    var known = new HashSet<ushort>();
    foreach (string candidate in new[] { Path.Combine(dataFolder, "tables", "PassiveSkills.json"), "data/tables/PassiveSkills.json" })
    {
        if (!File.Exists(candidate)) continue;
        using var stream = File.OpenRead(candidate);
        using var json = JsonDocument.Parse(stream);
        foreach (JsonElement row in json.RootElement.EnumerateArray())
        {
            if (!row.TryGetProperty("PassiveSkillGraphId", out var idElement) || !idElement.TryGetUInt16(out ushort id) || id == 0) continue;
            known.Add(id);
            if (row.TryGetProperty("AscendancyKey", out var key) && key.ValueKind == JsonValueKind.Number) ascendancyIds.Add(id);
        }
        break;
    }
    foreach (ushort node in nodes)
    {
        if (known.Count > 0 && !known.Contains(node)) unknown.Add(node);
        else if (ascendancyIds.Contains(node)) ascendancy.Add(node);
        else ordinary.Add(node);
    }
    return (ordinary, ascendancy, unknown);
}

static int ClassIndex(string name) => name switch
{
    "Scion" => 0, "Marauder" => 1, "Ranger" => 2, "Witch" => 3, "Duelist" => 4, "Templar" => 5, "Shadow" => 6,
    _ => -1,
};

static string ClassName(int index) => index switch
{
    0 => "Scion", 1 => "Marauder", 2 => "Ranger", 3 => "Witch", 4 => "Duelist", 5 => "Templar", 6 => "Shadow",
    _ => "",
};

static string Localised(string className) => className switch
{
    "Scion" => "（贵族）", "Marauder" => "（野蛮人）", "Ranger" => "（游侠）", "Witch" => "（女巫）",
    "Duelist" => "（决斗者）", "Templar" => "（圣堂武僧）", "Shadow" => "（暗影）", _ => "",
};

static int Fail(string reason)
{
    Console.Error.Write(reason);
    return 1;
}
