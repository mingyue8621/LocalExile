using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer.Core;
using GameServer.Data.Ggpk;

namespace LocalExile;

/// <summary>extract-data.ps1 for a release: the same manifest (written into the package when it is built, with each entry's
/// program), the same stamps in data\extract-stamps.json and the same rule for what runs. Read-only on the GGPK.</summary>
static class Extraction
{
    static readonly string ManifestFile = Path.Combine("Tools", "extract-manifest.json");
    static readonly string StampFile = Path.Combine("data", "extract-stamps.json");

    sealed record Entry(string Name, string[] Outputs, string Runner, string Exe, string[] Args, int Version, string[] Needs);
    sealed record Stamp([property: JsonPropertyName("version")] int Version, [property: JsonPropertyName("at")] string At);

    public static void Run(Output log)
    {
        log.Step("extract", "checking the game data ...");
        var manifest = JsonSerializer.Deserialize<Entry[]>(File.ReadAllText(ManifestFile)) ?? [];
        var stamps = ReadStamps(log);

        var todo = new List<(Entry Entry, string Why)>();
        foreach (var entry in manifest)
        {
            string? why = Why(entry, stamps);
            why ??= entry.Needs.FirstOrDefault(n => todo.Any(t => t.Entry.Name == n)) is { } needed ? $"{needed} reruns" : null;
            if (why is not null) todo.Add((entry, why));
        }
        if (todo.Count == 0)
        {
            log.Info("game data is up to date");
            return;
        }

        // A server install has no client to read: its data files come from the machine that built it. Nothing to remake is
        // fine (above); something to remake is only a warning when the file is already there in an older version, and an
        // error naming the setting when it is missing outright — never the tool's stack trace for a folder that is simply
        // not on this machine (live 2026-11-12: the client moved to G:\POEClient, and extraction died on "Client").
        string client = ServerConfig.ClientFolder;
        if (ClientFiles.Resolve(client) is null)
        {
            var missing = todo.Where(t => !File.Exists(Local(t.Entry.Outputs[0]))).ToList();
            if (missing.Count > 0)
                throw new LauncherException(ExitCode.Extraction,
                    $"no game client at \"{client}\" (Client.Folder in {ServerConfig.Path}) to make "
                    + $"{string.Join(", ", missing.Select(m => m.Entry.Outputs[0]))} from. Point Client.Folder at the client, "
                    + "or copy those files from the machine that built this release.");
            foreach (var (entry, why) in todo)
                log.Warn($"{entry.Outputs[0]} stays at its older version ({why}): there is no game client at \"{client}\" "
                    + $"(Client.Folder) to remake it from");
            log.Info("continuing without remaking the game data");
            return;
        }

        log.Info($"{todo.Count} of {manifest.Length} data files to make from your client. A first run takes several minutes; each step is reported.");
        int done = 0;
        foreach (var (entry, why) in todo)
        {
            done++;
            log.Progress("extract", $"[{done}/{todo.Count}] {entry.Name} ({why}) ...", done - 1, todo.Count);
            var started = DateTime.Now;
            // A release ships no Python: every entry is one of its published programs (build-release.ps1 checks).
            string name = "extract-" + entry.Name;
            int code = Tools.Run(entry.Exe, entry.Args, name, log);
            string output = Local(entry.Outputs[0]);
            bool fresh = File.Exists(output) && File.GetLastWriteTime(output) >= started.AddSeconds(-2);
            if (code != 0 || !fresh)
                throw new LauncherException(ExitCode.Extraction, $"extracting {entry.Name} failed (exit {code}{(fresh ? "" : $", {output} not written")}; logs\\{name}.log):\n{Tools.Tail(Path.Combine("logs", name + ".log"))}");
            stamps[entry.Name] = new(entry.Version, DateTime.Now.ToString("s"));
            File.WriteAllText(StampFile, JsonSerializer.Serialize(stamps));
            log.Progress("extract", $"[{done}/{todo.Count}] {entry.Name} done in {(int)(DateTime.Now - started).TotalSeconds} s", done, todo.Count);
        }
    }

    /// <summary>The manifest's paths are extract-data.ps1's, with backslashes.</summary>
    static string Local(string path) => path.Replace('\\', Path.DirectorySeparatorChar);

    static string? Why(Entry entry, Dictionary<string, Stamp> stamps)
    {
        if (entry.Outputs.FirstOrDefault(o => !File.Exists(Local(o))) is { } missing) return $"{missing} missing";
        if (!stamps.TryGetValue(entry.Name, out var stamp)) return "no stamp";
        return stamp.Version != entry.Version ? $"version {stamp.Version} -> {entry.Version}" : null;
    }

    static Dictionary<string, Stamp> ReadStamps(Output log)
    {
        Directory.CreateDirectory("data");
        if (!File.Exists(StampFile)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, Stamp>>(File.ReadAllText(StampFile)) ?? []; }
        catch (JsonException)
        {
            log.Warn($"{StampFile} is unreadable; treating every file as unstamped");
            return [];
        }
    }
}
