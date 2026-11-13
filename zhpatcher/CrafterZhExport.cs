// 从客户端自带的中文数据导出物品制作器（Waypoint 的「工具 → 物品制作器」）需要的中文词表。
//
// 制作器的数据来自 Gateway 的 crafter 模式，是英文的（服务端的 data\ 没有译文）。客户端 GGPK 里本来就有同一批
// 数据的中文版：data\*.datc64 的 Name/Text2 列，以及 metadata\statdescriptions\*.txt 里每个词条的默认语言块。
// 这里按 Gateway 使用的键把它们导出成一份词典，制作器显示时查表：基础名按 Metadata/… Id，词缀名按 mod Id，
// 稀有名按 Wordlist+HASH32，词条按 stats 组合，类名按 ItemClass Id。查不到的键回落英文，所以缺条目只会让那一行
// 保持英文，不会显示错字。
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameServer.Data.Dat;
using GameServer.Data.Ggpk;
using GgDump;

namespace ZhPatcher;

/// <summary>物品制作器的中文词表导出（<c>zhpatcher --crafter-zh</c>）。</summary>
public static class CrafterZhExport
{
    /// <summary>客户端汉化表里是 “混沌石(Chaos Orb)”“野蛮之(of the Brute)”：去掉末尾括号里的英文。</summary>
    static readonly Regex TrailingEnglish = new(@"\s*\([A-Za-z0-9'’\-\.\, :&/!+%°]+\)\s*$", RegexOptions.Compiled);

    /// <summary>客户端的富文本：&lt;DA1&gt; 这类标签、{{…}} 分组（保留内部文字）。</summary>
    static readonly Regex RichTag = new(@"<[A-Za-z][^>]{0,16}>", RegexOptions.Compiled);

    /// <summary>没有文字只有标注的符号：词条分档的箭头、过滤器补丁的圆点。</summary>
    static readonly Regex Decoration = new(@"[▲▼▽△◆◇★☆●○■□◈♦→←↑↓▷◁]", RegexOptions.Compiled);

    /// <summary>行首的来源标签，如 “[▽·塑]”。</summary>
    static readonly Regex LeadingLabel = new(@"^\[[^\]]{1,12}\]\s*", RegexOptions.Compiled);

    public static int Run(string repoRoot, string clientDir, string outFile, Action<string> log)
    {
        var tools = Patcher.FindOodleTools(repoRoot)
            ?? throw new InvalidOperationException("找不到 Oodle 解码器（Tools\\node\\node.exe 与 Tools\\ooz\\ooz_worker.mjs）");
        string schemaPath = Path.Combine(repoRoot, "data", "schema", "schema.min.json");
        if (!File.Exists(schemaPath)) throw new FileNotFoundException("找不到 data\\schema\\schema.min.json", schemaPath);
        var schema = DatSchema.LoadFile(schemaPath);

        using var decoder = new NodeOodleDecoder(tools.Node, tools.Worker);
        using var fs = GameFileSystem.Open(Path.Combine(clientDir, "Content.ggpk"), decoder);
        var paths = fs.EnumeratePaths().ToList();

        var bases = Table(fs, paths, schema, "data/baseitemtypes.datc64", "BaseItemTypes", "Id", "Name");
        var classes = Table(fs, paths, schema, "data/itemclasses.datc64", "ItemClasses", "Id", "Name");
        var mods = Table(fs, paths, schema, "data/mods.datc64", "Mods", "Id", "Name");

        // 稀有名的单词：Text2 是中文，Text 是英文（旧客户端可能没有 Text2）。
        var words = new Dictionary<string, string>(StringComparer.Ordinal);
        if (schema.Values.FirstOrDefault(t => t.Name.Equals("Words", StringComparison.OrdinalIgnoreCase)) is { } wordTable
            && paths.Contains("data/words.datc64", StringComparer.OrdinalIgnoreCase))
            foreach (var row in DatFile.Parse(fs.ReadFile("data/words.datc64")).ReadRows(wordTable))
            {
                long list = row["Wordlist"].Raw is long l ? l : 0;
                ulong hash = row["HASH32"].Raw is ulong h ? h : 0;
                string text = CleanName(row.GetValueOrDefault("Text2").Raw as string);
                if (text.Length == 0) text = CleanName(row.GetValueOrDefault("Text").Raw as string);
                if (list is 1 or 2 && text.Length > 0) words[$"{list}/{hash}"] = text;
            }

        var descs = Descriptions(fs, paths, log, out int blocks, out int chinese);

        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["source"] = $"Path of Exile 客户端中文数据（导出 {DateTime.Now:yyyy-MM-dd HH:mm}）",
            ["classes"] = classes,
            ["bases"] = bases,
            ["mods"] = mods,
            ["words"] = words,
            ["descs"] = descs,
        }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        // 制作器直接读 .gz（Waypoint 的 src\Waypoint\Data\crafter-zh.json.gz）。
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
        using (var file = File.Create(outFile))
        using (var gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
            writer.Write(json);

        log($"类名 {classes.Count}，基础类型 {bases.Count}，词缀 {mods.Count}，稀有名 {words.Count}");
        log($"词条块 {blocks}，其中中文 {chinese}，写入 {descs.Count} 条");
        log($"已写入 {outFile}（{new FileInfo(outFile).Length / 1024} KB）");
        return 0;
    }

    /// <summary>一张本地化表的两列：按 Id 取中文名。</summary>
    static Dictionary<string, string> Table(GameFileSystem fs, List<string> paths, IReadOnlyDictionary<string, SchemaTable> schema,
        string path, string tableName, string idColumn, string nameColumn)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (schema.Values.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase)) is not { } table
            || !paths.Contains(path, StringComparer.OrdinalIgnoreCase)) return map;
        foreach (var row in DatFile.Parse(fs.ReadFile(path)).ReadRows(table))
        {
            string id = row.GetValueOrDefault(idColumn).Raw as string ?? "";
            string name = CleanName(row.GetValueOrDefault(nameColumn).Raw as string);
            if (id.Length > 0 && name.Length > 0) map[id] = name;
        }
        return map;
    }

    /// <summary>metadata\statdescriptions\*.txt 的每个 description 块（汉化后默认语言块即中文）。</summary>
    static List<object> Descriptions(GameFileSystem fs, List<string> paths, Action<string> log, out int blocks, out int chinese)
    {
        var descs = new List<object>();
        blocks = chinese = 0;
        var files = paths.Where(p => p.StartsWith("metadata/statdescriptions/", StringComparison.OrdinalIgnoreCase)
                                     && p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToList();
        if (files.Count == 0) log("⚠ 客户端里没有 metadata\\statdescriptions\\，词条只能显示英文");
        foreach (string path in files)
        {
            byte[] raw = fs.ReadFile(path);
            int skip = raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE ? 2 : 0;
            string[] lines = Encoding.Unicode.GetString(raw, skip, raw.Length - skip).Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                if (!IsHeader(lines[i].Trim())) continue;
                blocks++;
                if (i + 2 >= lines.Length) continue;
                string[] head = lines[i + 1].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (head.Length < 2 || !int.TryParse(head[0], out int statCount)) continue;
                var stats = head.Skip(1).Take(statCount).ToList();
                if (!int.TryParse(lines[i + 2].Trim(), out int variants)) continue;
                var list = new List<object>();
                bool hasChinese = false, bad = false;
                for (int v = 0; v < variants; v++)
                {
                    string? line = i + 3 + v < lines.Length ? lines[i + 3 + v] : null;
                    int open = line?.IndexOf('"') ?? -1, close = line?.LastIndexOf('"') ?? -1;
                    if (line is null || open < 0 || close <= open) { bad = true; break; }
                    string when = line[..open].Trim();
                    string text = Strip(line[(open + 1)..close]);
                    if (HasChinese(text)) hasChinese = true;
                    // 引号之后是 “handler 1” 对，到 reminderstring 为止
                    string rest = line[(close + 1)..];
                    int reminder = rest.IndexOf("reminderstring", StringComparison.Ordinal);
                    if (reminder >= 0) rest = rest[..reminder];
                    var tokens = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    var handlers = new List<object>();
                    for (int t = 0; t + 1 < tokens.Length; t += 2)
                        if (int.TryParse(tokens[t + 1], out int which)) handlers.Add(new object[] { tokens[t], which });
                    list.Add(new Dictionary<string, object?>
                    {
                        ["when"] = when.Length == 0 ? Array.Empty<string>() : when.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries),
                        ["text"] = text,
                        ["handlers"] = handlers,
                    });
                }
                if (bad || list.Count == 0) continue;
                if (hasChinese) chinese++;
                descs.Add(new Dictionary<string, object?> { ["stats"] = stats, ["variants"] = list });
                i += 2 + variants;
            }
        }
        return descs;
    }

    static bool IsHeader(string line) => line == "description" || line.StartsWith("description ", StringComparison.Ordinal);

    static bool HasChinese(string s) => s.Any(c => c is >= '\u4e00' and <= '\u9fff');

    /// <summary>去掉客户端标记，只留文字。</summary>
    static string Strip(string s)
    {
        s = s.Replace("{{", "").Replace("}}", "");
        s = RichTag.Replace(s, "");
        s = Decoration.Replace(s, "");
        return LeadingLabel.Replace(s.Trim(), "").Trim();
    }

    /// <summary>中文名：去掉末尾括号里的英文与装饰符号。</summary>
    static string CleanName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string text = raw.Trim();
        for (int i = 0; i < 3 && TrailingEnglish.IsMatch(text); i++) text = TrailingEnglish.Replace(text, "");
        return Decoration.Replace(LeadingLabel.Replace(text, ""), "").Trim();
    }
}
