using Avalonia;
using System;
using System.Linq;

namespace LearnSomething.Client;

sealed class Program
{
    // True only when the OS itself launched the process at login (the
    // LaunchAgent/registry Run entry passes this flag) — never when the user
    // double-clicks or otherwise starts the app by hand.
    public static bool IsAutoStartLaunch { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        IsAutoStartLaunch = args.Contains("--autostart");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
