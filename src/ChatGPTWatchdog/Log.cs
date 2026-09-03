namespace ChatGPTWatchdog;

internal static class Log
{
    private static readonly object SyncRoot = new();

    public static void Write(string dataDirectory, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(dataDirectory);
                File.AppendAllText(
                    Path.Combine(dataDirectory, "watchdog.log"),
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never take down the watchdog.
        }
    }
}

