// 把 ggpk 自带的繁体中文数据转成简体并替换英文数据，让国际服客户端直接显示简体中文。
// 管线：提取 data/traditional chinese/*.datc64 → 繁转简（逐字映射，字节长度不变）→
//       替换 data/*.datc64|datcl64 → 替换英文字体为 CJK 字体 → 刷新 client-constants root hash → config language=en
// 替换前的英文原版备份在 <work>\backup-en，可一键恢复。
using System.Text;
using System.Text.Json;
using GameServer.Data.Dat;
using GameServer.Data.Ggpk;
using GgDump;
using LibBundledGGPK3;
using LibBundle3;
using LibBundle3.Records;

namespace ZhPatcher;

/// <summary>逐字繁→简映射（opencc-js 单字转换生成，保持字符串字节长度不变）。</summary>
public static class SimplifiedMap
{
    public static Dictionary<char, char> Load()
    {
        using var stream = typeof(SimplifiedMap).Assembly.GetManifestResourceStream("ZhPatcher.t2s-map.json")
            ?? throw new InvalidOperationException("内嵌繁简映射表缺失");
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(new StreamReader(stream).ReadToEnd())!;
        var map = new Dictionary<char, char>(raw.Count);
        foreach (var (k, v) in raw)
            if (k.Length == 1 && v.Length == 1) map[k[0]] = v[0];
        return map;
    }
}

/// <summary>一键简体汉化管线。</summary>
public sealed class Patcher
{
    /// <summary>工具版本号（随功能更新递增）。</summary>
    public const string ToolVersion = "1.2.0";

    public required string RepoRoot { get; init; }
    public required string ClientDir { get; init; }
    public required string WorkDir { get; init; }
    public Action<string> Log { get; init; } = _ => { };

    private string Ggpk => Path.Combine(ClientDir, "Content.ggpk");

    /// <summary>Oodle 解码器。客户端本身不带 oo2core.dll（3.29.3c 起游戏把 Oodle 编进可执行文件），所以用服务端
    /// 自带的那套：<c>Tools\node\node.exe</c> + <c>Tools\ooz\ooz_worker.mjs</c>。</summary>
    NodeOodleDecoder NewDecoder()
    {
        var tools = _oodleTools ??= FindOodleTools(RepoRoot);
        if (tools is not { } found) throw new InvalidOperationException(
            "找不到 Oodle 解码器：需要 Tools\\node\\node.exe 与 Tools\\ooz\\ooz_worker.mjs（服务端安装目录里有），"
            + "或用环境变量 ZHPATCHER_NODE / ZHPATCHER_OOZ 指定");
        return new NodeOodleDecoder(found.Node, found.Worker);
    }

    private (string Node, string Worker)? _oodleTools;

    /// <summary>node 与 ooz worker：环境变量、仓库自己的 Tools、与仓库并列的安装目录（G:\LocalExile 与 G:\POE1.1
    /// 这种布局），最后才是 PATH 上的 node。</summary>
    public static (string Node, string Worker)? FindOodleTools(string repoRoot)
    {
        if (Environment.GetEnvironmentVariable("ZHPATCHER_NODE") is { Length: > 0 } node
            && Environment.GetEnvironmentVariable("ZHPATCHER_OOZ") is { Length: > 0 } worker
            && File.Exists(node) && File.Exists(worker)) return (node, worker);

        var roots = new List<string> { repoRoot };
        string? parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(repoRoot)));
        if (parent is not null)
        {
            try { roots.AddRange(Directory.EnumerateDirectories(parent)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        foreach (string root in roots)
        {
            string nodePath = Path.Combine(root, "Tools", "node", "node.exe");
            string workerPath = Path.Combine(root, "Tools", "ooz", "ooz_worker.mjs");
            if (File.Exists(nodePath) && File.Exists(workerPath)) return (Path.GetFullPath(nodePath), Path.GetFullPath(workerPath));
        }
        string localWorker = Path.Combine(repoRoot, "Tools", "ooz", "ooz_worker.mjs");
        return File.Exists(localWorker) ? ("node", Path.GetFullPath(localWorker)) : null;
    }
    private string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Path of Exile", "production_Config.ini");
    private string BackupEn => Path.Combine(WorkDir, "backup-en");
    private string TwDir => Path.Combine(WorkDir, "tw");
    private string ScDir => Path.Combine(WorkDir, "sc");

    public const string FontSource = "art/2dart/fonts/spoqahansansneo-regular-poe.ttf";
    /// <summary>内嵌粗体 CJK 字体（Noto Sans SC Bold，wght=700 静态实例，TrueType 轮廓）。</summary>
    public const string BoldFontResource = "ZhPatcher.cjk-font-bold.ttf";
    public const string StatDescPrefix = "metadata/statdescriptions/";
    public static readonly string[] FontsReplaced =
    [
        "art/2dart/fonts/frizquadrataitc.ttf",
        "art/2dart/fonts/fontin-regular.ttf",
        "art/2dart/fonts/fontin-bold.ttf",
        "art/2dart/fonts/fontin-italic.ttf",
        "art/2dart/fonts/fontin-smallcaps.ttf",
        "art/2dart/fonts/optimusprincepssemibold.ttf",
    ];

    /// <summary>定位仓库与客户端。clientOverride 为用户手动指定的客户端目录（须含 Content.ggpk）。</summary>
    public static (string repoRoot, string clientDir)? Locate(string? clientOverride = null)
    {
        string? repo = FindRepoRoot();
        if (repo is null) return null;

        string? client = null;
        // 用户手动指定的目录优先（记下来，下次不用再选）
        if (clientOverride is not null && HoldsGgpk(clientOverride))
        {
            client = Path.GetFullPath(clientOverride);
            RememberClient(repo, client);
        }
        // 上次在界面里选的、服务端自己的 Client.Folder、最后才是仓库内的 Client/ 与 Path of Exile/
        client ??= SavedClient(repo) ?? ConfigClient(repo);
        if (client is null)
            foreach (var name in new[] { "Path of Exile", "Client" })
                if (HoldsGgpk(Path.Combine(repo, name))) { client = Path.GetFullPath(Path.Combine(repo, name)); break; }
        return client is null ? null : (repo, client);
    }

    /// <summary>The install or checkout this tool sits in: <c>GameServer.slnx</c> for a checkout, <c>release.json</c> with
    /// <c>LocalExile.exe</c> for a release, and <c>data/schema/schema.min.json</c> as the older marker (the tables moved into
    /// client.db, so a release may no longer ship it).</summary>
    public static string? FindRepoRoot()
    {
        for (var d = AppContext.BaseDirectory; d is not null; d = Path.GetDirectoryName(d))
            if (File.Exists(Path.Combine(d, "GameServer.slnx"))
                || File.Exists(Path.Combine(d, "release.json")) && File.Exists(Path.Combine(d, "LocalExile.exe"))
                || File.Exists(Path.Combine(d, "data", "schema", "schema.min.json"))) return d;
        return null;
    }

    static bool HoldsGgpk(string? dir) =>
        dir is { Length: > 0 } && File.Exists(Path.Combine(dir, "Content.ggpk"));

    /// <summary>The client folder the tool last used: <c>&lt;repo&gt;\.tmp\zhpatcher\client.txt</c>. Null when it is gone.</summary>
    public static string? SavedClient(string repoRoot)
    {
        try
        {
            string file = Path.Combine(repoRoot, ".tmp", "zhpatcher", "client.txt");
            return File.Exists(file) && File.ReadAllText(file).Trim() is { Length: > 0 } saved && HoldsGgpk(saved) ? saved : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    /// <summary>Remembers the client folder, so the next start needs no browsing.</summary>
    public static void RememberClient(string repoRoot, string clientDir)
    {
        try
        {
            string file = Path.Combine(repoRoot, ".tmp", "zhpatcher", "client.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, Path.GetFullPath(clientDir));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    /// <summary>The folder the servers themselves patch: <c>Client.Folder</c> in <c>Config/localexile.conf</c>, relative to
    /// the install or absolute; <c>auto</c> (the shipped default) follows <c>data/client-folder.txt</c> — the folder the
    /// login launcher last ran in — and then <c>Client\</c>. Null when it names no folder that holds a GGPK.</summary>
    public static string? ConfigClient(string repoRoot)
    {
        string configured = "";
        try
        {
            string conf = Path.Combine(repoRoot, "Config", "localexile.conf");
            if (File.Exists(conf))
                foreach (string line in File.ReadLines(conf))
                {
                    string text = line.Trim();
                    if (text.Length == 0 || text[0] == '#') continue;
                    int eq = text.IndexOf('=');
                    if (eq > 0 && text[..eq].Trim().Equals("Client.Folder", StringComparison.OrdinalIgnoreCase))
                    {
                        configured = text[(eq + 1)..].Trim().Trim('"');
                        break;
                    }
                }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        if (configured.Length > 0 && !configured.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            string full = Path.GetFullPath(Path.Combine(repoRoot, configured));
            if (HoldsGgpk(full)) return full;
        }
        try
        {
            string note = Path.Combine(repoRoot, "data", "client-folder.txt");
            if (File.Exists(note) && File.ReadAllText(note).Trim().Trim('"') is { Length: > 0 } named && HoldsGgpk(named))
                return Path.GetFullPath(named);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        string beside = Path.Combine(repoRoot, "Client");
        return HoldsGgpk(beside) ? beside : null;
    }

    /// <summary>读取客户端版本号。优先 logs/LatestClient.txt，兜底 Client.txt 尾部（如 3.29.3c）；exe 无版本资源。</summary>
    public static string? DetectClientVersion(string clientDir)
    {
        string latestLog = Path.Combine(clientDir, "logs", "LatestClient.txt");
        if (File.Exists(latestLog))
        {
            try
            {
                // 客户端运行时日志被占用，用 FileShare.ReadWrite 打开
                using var stream = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1 << 16);
                for (int i = 0; i < 200 && !reader.EndOfStream; i++)
                {
                    var line = reader.ReadLine();
                    if (line is null) break;
                    var ver = VersionFromUserAgent(line);
                    if (ver is not null) return ver;
                }
            }
            catch { /* 读不动就走 fallback */ }
        }

        // 兜底：Client.txt 尾部 64KB（最近一次会话的 User agent 行）
        string clientLog = Path.Combine(clientDir, "logs", "Client.txt");
        if (File.Exists(clientLog))
        {
            try
            {
                using var stream = new FileStream(clientLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long length = stream.Length;
                int tail = (int)Math.Min(1 << 16, length);
                stream.Seek(-tail, SeekOrigin.End);
                var buffer = new byte[tail];
                int read = 0;
                while (read < tail)
                {
                    int n = stream.Read(buffer, read, tail - read);
                    if (n <= 0) break;
                    read += n;
                }
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                string? last = null;
                for (int idx = text.IndexOf("release/tags/", StringComparison.Ordinal); idx >= 0; idx = text.IndexOf("release/tags/", idx + 1, StringComparison.Ordinal))
                    last = VersionFromUserAgent(text[idx..]);
                if (last is not null) return last;
            }
            catch { /* 忽略 */ }
        }
        return null;
    }

    private static string? VersionFromUserAgent(string text)
    {
        int idx = text.IndexOf("release/tags/", StringComparison.Ordinal);
        if (idx < 0) return null;
        var rest = text[(idx + "release/tags/".Length)..].TrimEnd();
        int end = 0;
        while (end < rest.Length && !char.IsWhiteSpace(rest[end])) end++;
        rest = rest[..end].TrimEnd('.');
        return rest.Length > 0 ? rest : null;
    }

    public static IEnumerable<string> RunningClientProcesses()
    {
        foreach (var name in new[] { "Game.patched", "PathOfExile", "Client" })
        {
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName(name))
            {
                proc.Dispose();
                yield return name + ".exe";
            }
        }
    }

    /// <summary>实际测试 ggpk 能否以读写方式打开（比进程名检测更准确：僵尸进程不持有文件锁）。</summary>
    public bool GgpkWritable()
    {
        try
        {
            using var fs = File.Open(Ggpk, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch { return false; }
    }

    public void Run(Action? betweenSteps = null)
    {
        var map = SimplifiedMap.Load();
        Directory.CreateDirectory(WorkDir);

        Log("① 提取 ggpk 内繁体中文数据 …");
        var twFiles = ExtractTw();
        Log($"   提取完成：{twFiles.Count} 个文件");

        Log("② 繁转简（逐字映射，字节长度不变）…");
        var scFiles = ConvertAll(twFiles, map);
        Log($"   转换完成：{scFiles.Count} 个文件");

        Log("③ 备份英文原版（仅首次）…");
        int backed = BackupCurrent(twFiles);
        Log($"   新备份 {backed} 个文件（累计见 {BackupEn}）");

        Log("④ 写回 ggpk 替换英文数据 …");
        ReplaceInGgpk(scFiles);
        Log("   数据替换完成");

        Log("⑤ 替换词缀/天赋/技能石描述（statdescriptions 多语言块对换）…");
        int descChanged = ProcessStatDescriptions();
        Log($"   已处理 {descChanged} 个描述文件");

        Log("⑥ 替换 UI 字体为 CJK 字体 …");
        ReplaceFonts();
        Log("   字体替换完成");

        Log("⑦ 刷新 client-constants root hash …");
        RefreshRootHash();
        Log("   root hash 已更新");

        Log("⑧ 设置客户端语言为 English（读 data/ 主路径的简体数据）…");
        SetLanguageEn();
        Log("   配置已就绪");
        Log("✅ 全部完成！启动客户端即可看到简体中文。");
    }

    /// <summary>只读检查：汉化管线要用的三类东西在客户端里是否都在，一个字节都不写。</summary>
    public void Check()
    {
        using var decoder = NewDecoder();
        using var fs = GameFileSystem.Open(Ggpk, decoder);
        var paths = new HashSet<string>(fs.EnumeratePaths(), StringComparer.OrdinalIgnoreCase);
        int tw = paths.Count(p => p.StartsWith("data/traditional chinese/", StringComparison.OrdinalIgnoreCase));
        var missingFonts = FontsReplaced.Where(f => !paths.Contains(f)).ToList();
        int desc = paths.Count(p => p.StartsWith(StatDescPrefix, StringComparison.OrdinalIgnoreCase));
        long size = new FileInfo(Ggpk).Length;

        Log($"服务端目录：{RepoRoot}");
        Log($"客户端：{ClientDir}（版本 {DetectClientVersion(ClientDir) ?? "未知"}）");
        Log($"GGPK：{Ggpk}（{size >> 30} GB）");
        Log(tw > 0
            ? $"繁体中文数据：{tw} 个文件  ✅"
            : "繁体中文数据：0 个文件  ❌ 客户端里没有 data/traditional chinese/，汉化无法进行");
        Log(missingFonts.Count == 0
            ? $"UI 字体：{FontsReplaced.Length}/{FontsReplaced.Length} 个  ✅"
            : $"UI 字体：缺 {missingFonts.Count} 个  ⚠ {string.Join(", ", missingFonts)}");
        Log(desc > 0
            ? $"词缀/天赋描述：{desc} 个文件  ✅"
            : "词缀/天赋描述：0 个  ⚠ 第⑤步会跳过（其余照做）");
        Log(tw > 0 && missingFonts.Count == 0 ? "✅ 可以汉化：--run" : "⚠ 见上面的缺失项，--run 可能失败或只做一半");
    }

    public void RestoreEnglish()
    {
        var backupFiles = Directory.Exists(BackupEn) ? Directory.EnumerateFiles(BackupEn, "*", SearchOption.AllDirectories).ToList() : [];
        if (backupFiles.Count == 0) throw new InvalidOperationException("没有英文备份可恢复（备份目录为空）");

        Log($"恢复 {backupFiles.Count} 个英文原版文件 …");
        using var ggpk = new BundledGGPK(Ggpk, parsePathsInIndex: false);
        int ok = 0;
        foreach (var file in backupFiles)
        {
            string rel = Path.GetRelativePath(BackupEn, file).Replace('\\', '/');
            // dat 文件备份在 backup-en 根下，对应 ggpk 的 data/ 路径；描述/字体备份路径本身已是完整 ggpk 路径
            string ggpkPath = rel.Contains('/') ? rel : $"data/{rel}";
            if (!ggpk.Index.TryGetFile(ggpkPath, out var fr)) { Log($"  跳过（ggpk 中不存在）：{ggpkPath}"); continue; }
            fr.Write(File.ReadAllBytes(file), saveIndex: false);
            ok++;
        }
        ggpk.Index.Save(Oodle.Compressor.Kraken, Oodle.CompressionLevel.Normal);
        Log($"已恢复 {ok} 个文件，刷新 root hash …");
        RefreshRootHash();
        Log("✅ 英文原版已恢复。");
    }

    private List<string> ExtractTw()
    {
        Directory.CreateDirectory(TwDir);
        using var decoder = NewDecoder();
        using var fs = GameFileSystem.Open(Ggpk, decoder);
        var files = new List<string>();
        foreach (var path in fs.EnumeratePaths())
        {
            if (!path.StartsWith("data/traditional chinese/", StringComparison.OrdinalIgnoreCase)) continue;
            string rel = path["data/traditional chinese/".Length..];
            string outPath = Path.Combine(TwDir, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllBytes(outPath, fs.ReadFile(path));
            files.Add(outPath);
        }
        return files;
    }

    private List<string> ConvertAll(List<string> twFiles, Dictionary<char, char> map)
    {
        Directory.CreateDirectory(ScDir);
        string schemaPath = Path.Combine(RepoRoot, "data", "schema", "schema.min.json");
        var schema = DatSchema.LoadFile(schemaPath);
        var byLower = new Dictionary<string, SchemaTable>(StringComparer.Ordinal);
        foreach (var t in schema.Values) byLower[t.Name.ToLowerInvariant()] = t;

        var outFiles = new List<string>();
        int failed = 0;
        foreach (var path in twFiles)
        {
            try
            {
                byte[] file = File.ReadAllBytes(path);
                ConvertOne(file, Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), byLower, map);
                string outPath = Path.Combine(ScDir, Path.GetRelativePath(TwDir, path));
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                File.WriteAllBytes(outPath, file);
                outFiles.Add(outPath);
            }
            catch (Exception ex)
            {
                failed++;
                Log($"  转换失败 {Path.GetFileName(path)}: {ex.Message}");
            }
        }
        if (failed > 0) throw new InvalidOperationException($"{failed} 个文件转换失败，已中止（未写入 ggpk）");
        return outFiles;
    }

    private static int ColumnSize(SchemaColumn c) => c.IsArray ? 16 : AtomSize(c.Type) * (c.IsInterval ? 2 : 1);

    private static int AtomSize(DatColumnType t) => t switch
    {
        DatColumnType.Bool => 1,
        DatColumnType.String => 8,
        DatColumnType.I16 => 2,
        DatColumnType.U16 => 2,
        DatColumnType.I32 => 4,
        DatColumnType.U32 => 4,
        DatColumnType.F32 => 4,
        DatColumnType.Row => 8,
        DatColumnType.EnumRow => 4,
        DatColumnType.ForeignRow => 16,
        DatColumnType.Array => 16,
        _ => 1,
    };

    /// <summary>与试验脚本相同的转换逻辑：按 schema 枚举字符串字段，只改这些字节。</summary>
    private static void ConvertOne(byte[] file, string stem, Dictionary<string, SchemaTable> byLower,
        Dictionary<char, char> map)
    {
        int rowCount = file[0] | file[1] << 8 | file[2] << 16 | file[3] << 24;
        int boundary = FindAlignedMagic(file, 4, rowCount);
        var variable = new byte[file.Length - 4 - boundary];
        Buffer.BlockCopy(file, 4 + boundary, variable, 0, variable.Length);
        var fixedData = new byte[boundary];
        Buffer.BlockCopy(file, 4, fixedData, 0, boundary);

        if (!byLower.TryGetValue(stem, out var table)) return;
        var columns = table.Columns;
        var offsets = new int[columns.Count];
        int expected = 0;
        for (int i = 0; i < columns.Count; i++) { offsets[i] = expected; expected += ColumnSize(columns[i]); }
        if (expected * rowCount != boundary) return; // schema 与数据不符时不动这个表
        int rowLength = rowCount > 0 ? boundary / rowCount : 0;

        var seen = new HashSet<int>();
        for (int r = 0; r < rowCount; r++)
        {
            int rowBase = r * rowLength;
            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                if (c.IsArray)
                {
                    if (c.Type != DatColumnType.String) continue;
                    uint len = U32(fixedData, rowBase + offsets[i]);
                    if (len == 0 || len > 100_000_000) continue;
                    int arrOff = (int)U32(fixedData, rowBase + offsets[i] + 8);
                    if (arrOff < 0 || arrOff + 4L * len > variable.Length) continue;
                    for (uint e = 0; e < len; e++)
                        ConvertStringAt(variable, (int)U32(variable, arrOff + (int)e * 4), map, seen);
                }
                else if (c.Type == DatColumnType.String)
                {
                    ConvertStringAt(variable, (int)U32(fixedData, rowBase + offsets[i]), map, seen);
                    if (c.IsInterval)
                        ConvertStringAt(variable, (int)U32(fixedData, rowBase + offsets[i] + 8), map, seen);
                }
            }
        }
        Buffer.BlockCopy(variable, 0, file, 4 + boundary, variable.Length);
    }

    private static void ConvertStringAt(byte[] variable, int off, Dictionary<char, char> map, HashSet<int> seen)
    {
        if (off < 0 || off >= variable.Length || !seen.Add(off)) return;
        int end = off;
        while (end + 4 <= variable.Length && (variable[end] | variable[end + 1] | variable[end + 2] | variable[end + 3]) != 0) end += 2;
        if (end + 4 > variable.Length || end == off || ((end - off) & 1) != 0) return;
        for (int i = off; i < end; i += 2)
        {
            char ch = (char)(variable[i] | variable[i + 1] << 8);
            if (map.TryGetValue(ch, out char rep))
            {
                variable[i] = (byte)rep;
                variable[i + 1] = (byte)((uint)rep >> 8);
            }
        }
    }

    /// <summary>把当前 ggpk 里每个将要替换的文件备份一份（只在第一次备份，之后不覆盖）。</summary>
    private int BackupCurrent(List<string> twFiles)
    {
        int backed = 0;
        using var decoder = NewDecoder();
        using var fs = GameFileSystem.Open(Ggpk, decoder);
        foreach (var tw in twFiles)
        {
            string rel = Path.GetRelativePath(TwDir, tw).Replace('\\', '/');
            string ggpkPath = $"data/{rel}";
            string backupPath = Path.Combine(BackupEn, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(backupPath)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.WriteAllBytes(backupPath, fs.ReadFile(ggpkPath));
            backed++;
        }
        // 字体备份
        foreach (var font in FontsReplaced)
        {
            string backupPath = Path.Combine(BackupEn, font.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(backupPath)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.WriteAllBytes(backupPath, fs.ReadFile(font));
            backed++;
        }
        return backed;
    }

    private void ReplaceInGgpk(IEnumerable<string> convertedFiles)
    {
        using var ggpk = new BundledGGPK(Ggpk, parsePathsInIndex: false);
        int ok = 0, missing = 0;
        foreach (var src in convertedFiles)
        {
            string name = Path.GetFileName(src);
            if (!ggpk.Index.TryGetFile($"data/{name}", out var fr)) { missing++; continue; }
            fr.Write(File.ReadAllBytes(src), saveIndex: false);
            ok++;
        }
        ggpk.Index.Save(Oodle.Compressor.Kraken, Oodle.CompressionLevel.Normal);
        if (missing > 0) Log($"  {missing} 个文件在 ggpk 中不存在（跳过）");
        Log($"  已写入 {ok} 个文件");
    }

    /// <summary>
    /// statdescriptions 文件是单文件多语言格式：每个条目的默认块（客户端语言 en 时显示）在前，
    /// 之后是 \tlang "X" 的各语言块。把默认块的内容行与 "Simplified Chinese" 块的内容行对换，
    /// 文件结构零变化，客户端读到的默认块即为官方简体中文文本。返回处理的文件数。
    /// </summary>
    private int ProcessStatDescriptions()
    {
        using var decoder = NewDecoder();
        using var fsRead = GameFileSystem.Open(Ggpk, decoder);
        var paths = fsRead.EnumeratePaths()
            .Where(p => p.StartsWith(StatDescPrefix, StringComparison.OrdinalIgnoreCase) && p.EndsWith(".txt"))
            .ToList();
        using var ggpk = new BundledGGPK(Ggpk, parsePathsInIndex: false);
        int changed = 0, noChinese = 0, entrySkips = 0;
        foreach (var path in paths)
        {
            byte[] raw = fsRead.ReadFile(path);

            // 备份英文原版（仅首次）
            string backupPath = Path.Combine(BackupEn, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(backupPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.WriteAllBytes(backupPath, raw);
            }

            string text = DecodeUtf16(raw);
            string swapped = SwapDefaultWithChinese(text, out bool any, out int skipped);
            if (!any) { noChinese++; if (skipped > 0) entrySkips += skipped; continue; }

            if (!ggpk.Index.TryGetFile(path, out var fr)) { Log($"  ggpk 中缺失（跳过）：{path}"); continue; }
            fr.Write(EncodeUtf16(swapped), saveIndex: false);
            changed++;
        }
        ggpk.Index.Save(Oodle.Compressor.Kraken, Oodle.CompressionLevel.Normal);
        if (noChinese > 0) Log($"  {noChinese} 个文件无简中块（原样保留）");
        if (entrySkips > 0) Log($"  {entrySkips} 个条目结构特殊，保留英文（避免解析错误）");
        return changed;
    }

    private static string DecodeUtf16(byte[] raw)
    {
        int start = raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE ? 2 : 0;
        return Encoding.Unicode.GetString(raw, start, raw.Length - start);
    }

    private static byte[] EncodeUtf16(string text)
    {
        var preamble = new byte[] { 0xFF, 0xFE };
        var body = Encoding.Unicode.GetBytes(text);
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, 2);
        Buffer.BlockCopy(body, 0, result, 2, body.Length);
        return result;
    }

    /// <summary>条目头判断：description / description &lt;id&gt; / handed_description &lt;id&gt; / no_description &lt;id&gt;。</summary>
    private static bool IsEntryHeader(string line)
    {
        var t = line.TrimEnd();
        return t == "description"
            || t.StartsWith("description ", StringComparison.Ordinal)
            || t.StartsWith("handed_description ", StringComparison.Ordinal)
            || t.StartsWith("no_description ", StringComparison.Ordinal);
    }

    private static bool IsStatIdList(string line)
    {
        // 深度 0 的 stat 标识符列表（如 main_hand_minimum_physical_damage main_hand_maximum_physical_damage）
        if (line.Length == 0) return false;
        foreach (var word in line.Split(' '))
        {
            if (word.Length == 0) return false;
            foreach (var c in word)
                if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '_')) return false;
        }
        return true;
    }

    /// <summary>把每个条目默认块的内容行与简中块内容行对换。any = 是否发生了替换；skipped = 因结构不干净而保留英文的条目数。</summary>
    public static string SwapDefaultWithChinese(string text, out bool any, out int skipped)
    {
        any = false;
        skipped = 0;
        var lines = text.Split("\r\n");
        var output = new List<string>(lines.Length);
        int i = 0;
        while (i < lines.Length)
        {
            if (IsEntryHeader(lines[i]))
            {
                int j = i + 1;
                var seg = new List<string> { lines[i] };
                // 条目在空行/纯空白行/下一个条目头处结束
                while (j < lines.Length && lines[j].Trim().Length > 0 && !IsEntryHeader(lines[j]))
                { seg.Add(lines[j]); j++; }
                bool hasLang = false;
                for (int k = 1; k < seg.Count; k++)
                    if (seg[k].TrimEnd().StartsWith("\tlang \"", StringComparison.Ordinal)) { hasLang = true; break; }
                if (SwapSegment(seg)) any = true;
                else if (hasLang) skipped++;
                output.AddRange(seg);
                // 分隔行原样保留
                while (j < lines.Length && lines[j].Trim().Length == 0) { output.Add(lines[j]); j++; }
                i = j;
            }
            else
            {
                output.Add(lines[i]);
                i++;
            }
        }
        return string.Join("\r\n", output);
    }

    private static bool SwapSegment(List<string> seg)
    {
        // lang 标记行可能带行尾空白（如 \tlang "Simplified Chinese"\t），用 TrimEnd 比较
        int firstLang = -1, zhLang = -1;
        for (int i = 1; i < seg.Count; i++)
        {
            var trimmed = seg[i].TrimEnd();
            if (trimmed.StartsWith("\tlang \"", StringComparison.Ordinal))
            {
                if (firstLang < 0) firstLang = i;
                if (trimmed == "\tlang \"Simplified Chinese\"") { zhLang = i; break; }
            }
        }
        if (zhLang < 0 || firstLang < 0) return false;

        // 结构校验：默认块必须干净。允许的行：
        //  - 深度>=1 的行（非空白）
        //  - 深度 0 的 stat 标识符行（handed_description 条目的主手 stat 列表）
        // 其余（空白行、中文/其他文本、嵌套头）一律跳过保留英文
        for (int i = 2; i < firstLang; i++)
        {
            var l = seg[i];
            if (l.Length == 0 || l.All(char.IsWhiteSpace)) return false;
            if (l.StartsWith('\t')) continue;
            if (IsStatIdList(l) && !IsEntryHeader(l)) continue;
            return false;
        }

        int zhEnd = zhLang + 1;
        while (zhEnd < seg.Count && !seg[zhEnd].TrimEnd().StartsWith("\tlang \"", StringComparison.Ordinal)) zhEnd++;
        // 简中块行可能带行尾空白（如 \t2\t），统一 TrimEnd
        var zhContent = seg.GetRange(zhLang + 1, zhEnd - (zhLang + 1)).Select(l => l.TrimEnd()).ToList();
        if (zhContent.Count == 0) return false;

        // 简中块内容行必须与默认块同构：首行是计数行（depth1 纯数字），其余为内容行（depth>=2）
        for (int i = 0; i < zhContent.Count; i++)
        {
            var l = zhContent[i];
            if (l.Length == 0 || l.All(char.IsWhiteSpace)) return false;
            if (l.StartsWith("\t\t")) continue;
            if (i == 0 && IsAllDigits(l.AsSpan(1))) continue;
            return false;
        }

        // 默认块替换起点：从 lang 行向前扫，跳过内容行（depth>=2），第一个 depth1 纯数字行即
        // 最后一个计数行（handed_description 条目有两个计数行，保留前面的 stat 列表）
        int defStart = -1;
        for (int k = firstLang - 1; k > 1; k--)
        {
            var l = seg[k];
            if (l.StartsWith("\t\t")) continue;
            if (l.StartsWith('\t') && IsAllDigits(l.AsSpan(1).TrimEnd())) defStart = k;
            break;
        }
        if (defStart < 0) return false;

        seg.RemoveRange(defStart, firstLang - defStart);
        seg.InsertRange(defStart, zhContent);
        return true;
    }

    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        if (span.Length == 0) return false;
        foreach (var c in span) if (c is < '0' or > '9') return false;
        return true;
    }

    private void ReplaceFonts()
    {
        using var decoder = NewDecoder();
        using var fs = GameFileSystem.Open(Ggpk, decoder);
        string fontPath = Path.Combine(WorkDir, "cjk-font-bold.ttf");

        // 优先使用内嵌粗体 CJK 字体；缺失时回退 ggpk 自带的常规字重
        if (File.Exists(fontPath))
        {
            Log("  使用粗体字体：Noto Sans SC Bold");
        }
        else
        {
            using var boldStream = typeof(SimplifiedMap).Assembly.GetManifestResourceStream(BoldFontResource)
                ?? throw new InvalidOperationException("内嵌粗体字体缺失");
            using var outFs = File.Create(fontPath);
            boldStream.CopyTo(outFs);
            Log("  使用粗体字体：Noto Sans SC Bold（内嵌）");
        }
        var content = File.ReadAllBytes(fontPath);

        using var ggpk = new BundledGGPK(Ggpk, parsePathsInIndex: false);
        foreach (var font in FontsReplaced)
        {
            if (!ggpk.Index.TryGetFile(font, out var fr)) { Log($"  字体缺失（跳过）：{font}"); continue; }
            fr.Write(content, saveIndex: false);
        }
        ggpk.Index.Save(Oodle.Compressor.Kraken, Oodle.CompressionLevel.Normal);
    }

    /// <summary>只重做字体替换 + root hash（调粗细后快速生效用）。</summary>
    public void ReplaceFontsOnly()
    {
        Directory.CreateDirectory(WorkDir);
        Log("⑥ 替换 UI 字体为粗体 CJK 字体 …");
        ReplaceFonts();
        Log("   字体替换完成");
        Log("⑦ 刷新 client-constants root hash …");
        RefreshRootHash();
        Log("   root hash 已更新");
        Log("✅ 字体已更新为粗体。");
    }

    private void RefreshRootHash()
    {
        string constantsPath = Path.Combine(RepoRoot, "data", "client-constants.json");
        if (!File.Exists(constantsPath)) { Log("  （data/client-constants.json 不存在，跳过——live.ps1 启动时会生成）"); return; }
        var constants = ClientConstants.Load(constantsPath) with { GgpkRootHash = Convert.ToHexString(GgpkMaintenance.RootHash(Ggpk)) };
        constants.Save(constantsPath);
        Log($"  新 root hash: {constants.GgpkRootHash.ToLowerInvariant()}");
    }

    public void SetLanguageEn()
    {
        if (!File.Exists(ConfigPath)) { Log($"  （未找到 {ConfigPath}，跳过）"); return; }
        var lines = File.ReadAllLines(ConfigPath).ToList();
        int section = lines.FindIndex(l => l.Trim().Equals("[LANGUAGE]", StringComparison.OrdinalIgnoreCase));
        if (section < 0)
        {
            lines.Add("[LANGUAGE]");
            lines.Add("language=en");
        }
        else
        {
            int key = -1;
            for (int i = section + 1; i < lines.Count && !lines[i].StartsWith('['); i++)
                if (lines[i].StartsWith("language=", StringComparison.OrdinalIgnoreCase)) { key = i; break; }
            if (key >= 0) lines[key] = "language=en";
            else lines.Insert(section + 1, "language=en");
        }
        File.WriteAllLines(ConfigPath, lines, new UTF8Encoding(false));
    }

    private static uint U32(byte[] d, int o) => (uint)(d[o] | d[o + 1] << 8 | d[o + 2] << 16 | d[o + 3] << 24);

    private static int FindAlignedMagic(byte[] file, int start, int rowCount)
    {
        for (int i = start; i + 8 <= file.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < 8; j++) if (file[i + j] != 0xBB) { match = false; break; }
            if (!match) continue;
            int boundary = i - start;
            if (rowCount == 0 || boundary % rowCount == 0) return boundary;
        }
        return -1;
    }
}
