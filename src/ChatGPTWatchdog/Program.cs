namespace ChatGPTWatchdog;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(
            initiallyOwned: true,
            name: @"Local\ChatGPTWatchdog.Singleton",
            createdNew: out var createdNew);

        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new WatchdogApplicationContext();
        Application.Run(context);
    }
}

