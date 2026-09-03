using System.Diagnostics;

namespace ChatGPTWatchdog;

internal static class ChatGptController
{
    private const string ProcessName = "ChatGPT";
    private const string AppUserModelId = "OpenAI.Codex_2p2nqsd0c76g0!App";

    public static bool IsRunning()
    {
        try
        {
            return Process.GetProcessesByName(ProcessName).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Start()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
            Arguments = $"shell:AppsFolder\\{AppUserModelId}",
            UseShellExecute = true
        });
    }
}

