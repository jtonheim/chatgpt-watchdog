using System.Diagnostics;

namespace ChatGPTWatchdog;

internal sealed class WatchdogApplicationContext : ApplicationContext
{
    private static readonly TimeSpan LaunchCooldown = TimeSpan.FromSeconds(30);

    private readonly SettingsStore _settingsStore = new();
    private readonly WatchdogSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _monitorItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly System.Windows.Forms.Timer _timer;
    private DateTimeOffset _nextLaunchAttempt = DateTimeOffset.MinValue;

    public WatchdogApplicationContext()
    {
        _settings = _settingsStore.Load();

        _statusItem = new ToolStripMenuItem("Checking ChatGPT status…")
        {
            Enabled = false
        };

        _monitorItem = new ToolStripMenuItem("Keep ChatGPT running")
        {
            Checked = _settings.MonitorEnabled
        };
        _monitorItem.Click += ToggleMonitoring;

        _startupItem = new ToolStripMenuItem("Start watchdog with Windows")
        {
            Checked = StartupRegistration.IsEnabled()
        };
        _startupItem.Click += ToggleStartup;

        var startNowItem = new ToolStripMenuItem("Start ChatGPT now");
        startNowItem.Click += (_, _) => StartChatGpt(manual: true);

        var openDataFolderItem = new ToolStripMenuItem("Open settings and log folder");
        openDataFolderItem.Click += (_, _) => OpenDataFolder();

        var exitItem = new ToolStripMenuItem("Exit watchdog");
        exitItem.Click += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([
            _statusItem,
            new ToolStripSeparator(),
            _monitorItem,
            _startupItem,
            new ToolStripSeparator(),
            startNowItem,
            openDataFolderItem,
            new ToolStripSeparator(),
            exitItem
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ChatGPT Watchdog",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => StartChatGpt(manual: true);

        _timer = new System.Windows.Forms.Timer
        {
            Interval = _settings.CheckIntervalSeconds * 1000
        };
        _timer.Tick += (_, _) => CheckChatGpt();
        _timer.Start();

        if (StartupRegistration.IsEnabled())
        {
            // Repair the Run entry if the installed path changed.
            StartupRegistration.Enable();
        }

        CheckChatGpt();
    }

    private void CheckChatGpt()
    {
        var running = ChatGptController.IsRunning();
        _statusItem.Text = running ? "ChatGPT is running" : "ChatGPT is not running";
        _trayIcon.Text = _settings.MonitorEnabled
            ? running ? "ChatGPT Watchdog — running" : "ChatGPT Watchdog — starting ChatGPT"
            : "ChatGPT Watchdog — monitoring disabled";

        if (_settings.MonitorEnabled && !running && DateTimeOffset.Now >= _nextLaunchAttempt)
        {
            StartChatGpt(manual: false);
        }
    }

    private void StartChatGpt(bool manual)
    {
        try
        {
            _nextLaunchAttempt = DateTimeOffset.Now + LaunchCooldown;
            ChatGptController.Start();
            Log.Write(_settingsStore.DataDirectory, manual
                ? "Manual ChatGPT launch requested."
                : "ChatGPT was not running; launch requested.");
        }
        catch (Exception exception)
        {
            Log.Write(_settingsStore.DataDirectory, $"Could not start ChatGPT: {exception.Message}");
            _trayIcon.ShowBalloonTip(
                5000,
                "ChatGPT Watchdog",
                "ChatGPT could not be started. See the watchdog log for details.",
                ToolTipIcon.Error);
        }
    }

    private void ToggleMonitoring(object? sender, EventArgs eventArgs)
    {
        _settings.MonitorEnabled = !_settings.MonitorEnabled;
        _monitorItem.Checked = _settings.MonitorEnabled;
        _settingsStore.Save(_settings);
        Log.Write(_settingsStore.DataDirectory,
            _settings.MonitorEnabled ? "Monitoring enabled." : "Monitoring disabled.");

        CheckChatGpt();
    }

    private void ToggleStartup(object? sender, EventArgs eventArgs)
    {
        try
        {
            if (StartupRegistration.IsEnabled())
            {
                StartupRegistration.Disable();
            }
            else
            {
                StartupRegistration.Enable();
            }

            _startupItem.Checked = StartupRegistration.IsEnabled();
            Log.Write(_settingsStore.DataDirectory,
                _startupItem.Checked ? "Windows startup enabled." : "Windows startup disabled.");
        }
        catch (Exception exception)
        {
            Log.Write(_settingsStore.DataDirectory, $"Could not change Windows startup: {exception.Message}");
            _trayIcon.ShowBalloonTip(
                5000,
                "ChatGPT Watchdog",
                "The Windows startup setting could not be changed.",
                ToolTipIcon.Error);
        }
    }

    private void OpenDataFolder()
    {
        Directory.CreateDirectory(_settingsStore.DataDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settingsStore.DataDirectory,
            UseShellExecute = true
        });
    }

    private void ExitApplication()
    {
        _timer.Stop();
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}

