using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChatGPTWatchdog;

internal enum ChatGptState
{
    NotRunning,
    BackgroundOnly,
    WindowOpen
}

internal static class ChatGptController
{
    private const string ProcessName = "ChatGPT";
    private const string AppUserModelId = "OpenAI.Codex_2p2nqsd0c76g0!App";
    private const int ExtendedWindowStyleIndex = -20;
    private const long ToolWindowStyle = 0x00000080L;
    private const long AppWindowStyle = 0x00040000L;
    private const uint OwnerWindow = 4;
    private const uint CloakedWindowAttribute = 14;

    private delegate bool EnumWindowsCallback(nint windowHandle, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint windowHandle);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint windowHandle, uint command);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        nint windowHandle,
        uint attribute,
        out int value,
        int valueSize);

    public static ChatGptState GetState() => GetStateForProcessName(ProcessName);

    internal static ChatGptState GetStateForProcessName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        try
        {
            if (processes.Length == 0)
            {
                return ChatGptState.NotRunning;
            }

            var processIds = processes.Select(process => (uint)process.Id).ToHashSet();
            var hasVisibleWindow = false;

            EnumWindows((windowHandle, _) =>
            {
                if (!AppearsInTaskbar(windowHandle))
                {
                    return true;
                }

                GetWindowThreadProcessId(windowHandle, out var processId);
                if (!processIds.Contains(processId))
                {
                    return true;
                }

                hasVisibleWindow = true;
                return false;
            }, nint.Zero);

            return hasVisibleWindow ? ChatGptState.WindowOpen : ChatGptState.BackgroundOnly;
        }
        catch
        {
            return ChatGptState.NotRunning;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
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

    private static bool AppearsInTaskbar(nint windowHandle)
    {
        if (!IsWindowVisible(windowHandle))
        {
            return false;
        }

        var extendedStyle = GetWindowLongPtr(windowHandle, ExtendedWindowStyleIndex).ToInt64();
        var isToolWindow = (extendedStyle & ToolWindowStyle) != 0;
        var isExplicitAppWindow = (extendedStyle & AppWindowStyle) != 0;
        var hasOwner = GetWindow(windowHandle, OwnerWindow) != nint.Zero;
        if (!isExplicitAppWindow && (isToolWindow || hasOwner))
        {
            return false;
        }

        var result = DwmGetWindowAttribute(
            windowHandle,
            CloakedWindowAttribute,
            out var cloaked,
            sizeof(int));

        return result != 0 || cloaked == 0;
    }
}
