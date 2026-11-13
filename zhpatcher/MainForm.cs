using ZhPatcher;

namespace ZhPatcher;

/// <summary>简体中文汉化桌面工具主界面。</summary>
public sealed class MainForm : Form
{
    private readonly TextBox _log = new();
    private readonly Button _patchButton = new();
    private readonly Button _restoreButton = new();
    private readonly TextBox _clientPathBox = new();
    private readonly Label _versionLabel = new();
    private (string repoRoot, string clientDir)? _located;

    public MainForm()
    {
        Text = $"流放之路 本地客户端 简体中文汉化工具 v{Patcher.ToolVersion}";
        Font = new Font("Microsoft YaHei UI", 9.5f);
        Size = new Size(880, 660);
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;

        var header = new Label
        {
            Text = "原理：把客户端 ggpk 自带的繁体中文数据转成简体后替换英文数据（客户端语言保持 English）。\r\n" +
                   "需要关闭游戏客户端后执行；服务端无需改动；客户端版本更新后需重新执行。",
            Dock = DockStyle.Top,
            Padding = new Padding(12, 10, 12, 8),
            Height = 64,
            ForeColor = Color.DimGray,
        };

        // 客户端目录选择行：标签 | 输入框 | 浏览按钮（TableLayoutPanel 保证对齐）
        var clientRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(12, 8, 12, 8),
            ColumnCount = 3,
            RowCount = 1,
        };
        clientRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        clientRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        clientRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        var clientLabel = new Label { Text = "客户端目录：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 6, 0) };
        _clientPathBox.Dock = DockStyle.Fill;
        _clientPathBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        var browseButton = new Button { Text = "浏览…", Dock = DockStyle.Fill, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(6, 0, 0, 0), UseVisualStyleBackColor = true };
        browseButton.Click += (_, _) => BrowseClientDir();
        _clientPathBox.TextChanged += (_, _) => ApplyManualClientPath(_clientPathBox.Text.Trim());
        clientRow.Controls.Add(clientLabel, 0, 0);
        clientRow.Controls.Add(_clientPathBox, 1, 0);
        clientRow.Controls.Add(browseButton, 2, 0);
        clientRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // 版本号行：工具版本 + 客户端版本
        _versionLabel.Dock = DockStyle.Top;
        _versionLabel.Padding = new Padding(12, 2, 12, 6);
        _versionLabel.Height = 26;
        _versionLabel.ForeColor = Color.Gray;
        _versionLabel.Text = $"工具版本 v{Patcher.ToolVersion}";

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(12, 6, 12, 6),
        };
        _patchButton.Text = "✦ 一键简体汉化";
        _patchButton.Size = new Size(190, 38);
        _patchButton.BackColor = Color.MediumSeaGreen;
        _patchButton.ForeColor = Color.White;
        _patchButton.FlatStyle = FlatStyle.Flat;
        _patchButton.Click += (_, _) => RunPatching();

        _restoreButton.Text = "↩ 恢复英文原版";
        _restoreButton.Size = new Size(170, 38);
        _restoreButton.FlatStyle = FlatStyle.Flat;
        _restoreButton.Click += (_, _) => RunRestore();

        var checkButton = new Button { Text = "🔍 只读检查", Size = new Size(132, 38), FlatStyle = FlatStyle.Flat };
        checkButton.Click += (_, _) => RunCheck();

        buttons.Controls.Add(_patchButton);
        buttons.Controls.Add(_restoreButton);
        buttons.Controls.Add(checkButton);

        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.White;
        _log.Font = new Font("Consolas", 9.5f);

        Controls.Add(_log);
        Controls.Add(buttons);
        Controls.Add(_versionLabel);
        Controls.Add(clientRow);
        Controls.Add(header);

        Load += (_, _) => Detect();
    }

    private void Detect()
    {
        _located = Patcher.Locate();
        if (_located is null)
        {
            _versionLabel.ForeColor = Color.Firebrick;
            _versionLabel.Text = FindRepoRoot() is not null
                ? $"工具版本 v{Patcher.ToolVersion}    ⚠ 未找到客户端（须含 Content.ggpk），点「浏览」选一下"
                : $"工具版本 v{Patcher.ToolVersion}    ❌ 未找到服务端目录，请把本工具放在服务端目录（含 GameServer.slnx 或 release.json + LocalExile.exe）内";
            _patchButton.Enabled = _restoreButton.Enabled = false;
            return;
        }
        var (_, client) = _located.Value;
        _clientPathBox.Text = client; // 会触发 ApplyManualClientPath（目录一致时无操作）
        RefreshVersionLabel(client);
    }

    private void RefreshVersionLabel(string clientDir)
    {
        string? clientVersion = Patcher.DetectClientVersion(clientDir);
        _versionLabel.Text = $"工具版本 v{Patcher.ToolVersion}    客户端版本：{clientVersion ?? "未知"}";
    }

    /// <summary>浏览选择客户端目录（须含 Content.ggpk）。</summary>
    private void BrowseClientDir()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择游戏客户端目录（目录下须有 Content.ggpk）",
            ShowNewFolderButton = false,
        };
        if (_clientPathBox.Text.Trim() is { Length: > 0 } current && Directory.Exists(current))
            dialog.SelectedPath = current;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string dir = dialog.SelectedPath;
        if (!File.Exists(Path.Combine(dir, "Content.ggpk")))
        {
            MessageBox.Show(this, "所选目录下没有 Content.ggpk，不是有效的客户端目录。", "目录无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _clientPathBox.Text = dir; // 触发 ApplyManualClientPath
    }

    /// <summary>手动输入/选择客户端目录后的校验与切换。</summary>
    private void ApplyManualClientPath(string dir)
    {
        // 空或与当前一致时不动
        if (dir.Length == 0 || (_located is not null && string.Equals(
                Path.GetFullPath(_located.Value.clientDir), Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase)))
            return;

        if (!Directory.Exists(dir) || !File.Exists(Path.Combine(dir, "Content.ggpk")))
        {
            _versionLabel.ForeColor = Color.Firebrick;
            _versionLabel.Text = $"工具版本 v{Patcher.ToolVersion}    ⚠ 目录无效（须含 Content.ggpk）";
            _patchButton.Enabled = _restoreButton.Enabled = false;
            return;
        }

        // 仓库根不变（工具所在位置决定），仅切换客户端目录
        var repo = _located?.repoRoot ?? FindRepoRoot();
        if (repo is null)
        {
            _versionLabel.ForeColor = Color.Firebrick;
            _versionLabel.Text = $"工具版本 v{Patcher.ToolVersion}    ❌ 未找到服务端目录，请把本工具放在服务端目录内";
            _patchButton.Enabled = _restoreButton.Enabled = false;
            return;
        }
        _located = (repo, dir);
        Patcher.RememberClient(repo, dir);
        _versionLabel.ForeColor = Color.Gray;
        RefreshVersionLabel(dir);
        _patchButton.Enabled = _restoreButton.Enabled = true;
        Log($"客户端目录已切换：{dir}");
    }

    private static string? FindRepoRoot() => Patcher.FindRepoRoot();

    private void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(message)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    }

    private async void RunPatching()
    {
        if (_located is null) { Log("路径未就绪"); return; }
        var (repo, client) = _located.Value;
        var patcher = new Patcher
        {
            RepoRoot = repo,
            ClientDir = client,
            WorkDir = Path.Combine(repo, ".tmp", "zhpatcher"),
            Log = Log,
        };
        var running = Patcher.RunningClientProcesses().ToList();
        if (running.Count > 0 && !patcher.GgpkWritable())
        {
            Log($"❌ 检测到客户端正在运行（{string.Join(", ", running)}）且 ggpk 被锁定，请先完全关闭游戏再执行。");
            return;
        }
        if (running.Count > 0)
            Log($"⚠ 检测到残留进程（{string.Join(", ", running)}），但 ggpk 未被锁定，继续执行。");

        SetBusy(true);
        _log.Clear();
        Log($"开始：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"工具版本 v{Patcher.ToolVersion}；客户端目录：{client}");
        try
        {
            await Task.Run(() => patcher.Run());
        }
        catch (Exception ex)
        {
            Log($"❌ 失败：{ex.Message}");
            Log("（未完成的写入不会影响客户端；可修复后重试）");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RunCheck()
    {
        if (_located is null) { Log("路径未就绪"); return; }
        var (repo, client) = _located.Value;
        var patcher = new Patcher
        {
            RepoRoot = repo,
            ClientDir = client,
            WorkDir = Path.Combine(repo, ".tmp", "zhpatcher"),
            Log = Log,
        };
        SetBusy(true);
        _log.Clear();
        Log("只读检查：不改动任何文件 …");
        try
        {
            await Task.Run(() => patcher.Check());
        }
        catch (Exception ex)
        {
            Log($"❌ 检查失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RunRestore()
    {
        if (_located is null) return;
        var (repo, client) = _located.Value;
        var patcher = new Patcher
        {
            RepoRoot = repo,
            ClientDir = client,
            WorkDir = Path.Combine(repo, ".tmp", "zhpatcher"),
            Log = Log,
        };
        var running = Patcher.RunningClientProcesses().ToList();
        if (running.Count > 0 && !patcher.GgpkWritable())
        {
            Log($"❌ 客户端正在运行（{string.Join(", ", running)}）且 ggpk 被锁定，请先关闭。");
            return;
        }
        if (running.Count > 0)
            Log($"⚠ 检测到残留进程（{string.Join(", ", running)}），但 ggpk 未被锁定，继续执行。");

        SetBusy(true);
        try
        {
            await Task.Run(() => patcher.RestoreEnglish());
        }
        catch (Exception ex)
        {
            Log($"❌ 失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _patchButton.Enabled = _restoreButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
