namespace ZhPatcher;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // 命令行模式：--version 显示版本，--run 执行汉化，--restore 恢复英文（无界面，便于脚本/排查）
        // 可选 --client <目录> 手动指定客户端目录（须含 Content.ggpk）
        if (args.Length > 0 && args[0] == "--version")
        {
            string? clientOverride = null;
            for (int i = 1; i + 1 < args.Length; i++)
                if (args[i] == "--client") clientOverride = args[i + 1];
            if (!AttachConsole(0xFFFFFFFFu)) AllocConsole();
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
            Console.WriteLine($"ZhPatcher v{Patcher.ToolVersion}");
            var loc = Patcher.Locate(clientOverride);
            if (loc is not null)
                Console.WriteLine($"客户端：{loc.Value.clientDir}（版本 {Patcher.DetectClientVersion(loc.Value.clientDir) ?? "未知"}）");
            else
                Console.WriteLine(Patcher.FindRepoRoot() is { } root
                    ? $"服务端目录：{root}\n未找到客户端：用 --client 指定含 Content.ggpk 的目录（指定一次后会记住）"
                    : "未找到服务端目录：请把本工具放在服务端目录（含 GameServer.slnx 或 release.json + LocalExile.exe）内");
            return 0;
        }
        if (args.Length > 0 && args[0] is "--run" or "--restore" or "--fonts" or "--check" or "--crafter-zh")
        {
            string? clientOverride = null;
            string? outFile = null;
            for (int i = 1; i + 1 < args.Length; i++)
            {
                if (args[i] == "--client") clientOverride = args[i + 1];
                if (args[i] == "--out") outFile = args[i + 1];
            }
            // 显式指定的客户端目录无效时报错退出，而不是静默回退到自动检测
            if (clientOverride is not null &&
                (!Directory.Exists(clientOverride) || !File.Exists(Path.Combine(clientOverride, "Content.ggpk"))))
            {
                if (!AttachConsole(0xFFFFFFFFu)) AllocConsole();
                try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
                Console.WriteLine($"--client 指定的目录无效（须含 Content.ggpk）：{clientOverride}");
                return 2;
            }
            return ConsoleMain(args[0], clientOverride, outFile);
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    static int ConsoleMain(string command, string? clientOverride = null, string? outFile = null)
    {
        // WinExe 默认没有控制台：附着到启动它的终端，或新建一个
        if (!AttachConsole(0xFFFFFFFFu)) AllocConsole();
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* 无终端时忽略 */ }
        var located = Patcher.Locate(clientOverride);
        if (located is null)
        {
            Console.WriteLine(Patcher.FindRepoRoot() is { } root
                ? $"服务端目录：{root}\n未找到客户端：用 --client 指定含 Content.ggpk 的目录（指定一次后会记住）"
                : "未找到服务端目录：请把本工具放在服务端目录（含 GameServer.slnx 或 release.json + LocalExile.exe）内。");
            return 2;
        }
        var (repo, client) = located.Value;
        Patcher.RememberClient(repo, client);
        Console.WriteLine($"工具版本 v{Patcher.ToolVersion}");
        Console.WriteLine($"仓库：{repo}\n客户端：{client}（版本 {Patcher.DetectClientVersion(client) ?? "未知"}）");
        var patcher = new Patcher
        {
            RepoRoot = repo,
            ClientDir = client,
            WorkDir = Path.Combine(repo, ".tmp", "zhpatcher"),
            Log = m => Console.WriteLine(m),
        };
        // 检查与导出都只读，不需要 ggpk 可写
        bool check = command is "--check" or "--crafter-zh";
        var running = Patcher.RunningClientProcesses().ToList();
        if (!check)
        {
            if (running.Count > 0 && !patcher.GgpkWritable())
            {
                Console.WriteLine($"客户端正在运行（{string.Join(", ", running)}）且 ggpk 被锁定，请先关闭。");
                return 3;
            }
            if (running.Count > 0)
                Console.WriteLine($"⚠ 检测到残留进程（{string.Join(", ", running)}），但 ggpk 未被锁定，继续执行。");
        }
        try
        {
            switch (command)
            {
                case "--check": patcher.Check(); break;
                case "--crafter-zh":
                    return CrafterZhExport.Run(repo, client, outFile ?? Path.Combine(repo, "crafter-zh.json.gz"), Console.WriteLine);
                case "--fonts": patcher.ReplaceFontsOnly(); break;
                case "--restore": patcher.RestoreEnglish(); break;
                default: patcher.Run(); break;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"失败：{ex.Message}");
            return 1;
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}
