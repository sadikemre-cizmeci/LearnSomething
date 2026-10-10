using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LearnSomething.Client.Data;
using LearnSomething.Client.Views;

namespace LearnSomething.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Default: quits once every window (card + settings, if open) has
            // closed. Closing just the card while Settings is still open
            // does not end the app — only closing both does.
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var services = AppStartup.InitializeAsync().GetAwaiter().GetResult();
            var cardHost = new CardWindowHost(services);

            if (Program.IsAutoStartLaunch)
            {
                // The OS started this process at login — go straight to the
                // card, no Settings in the way.
                cardHost.GetOrCreate().Show();
            }
            else
            {
                // The user launched the app by hand (first install, or
                // reopening after closing it) — always land on Settings so
                // they can confirm/pick a list before studying.
                var settingsWindow = new SettingsWindow(services, cardHost);
                settingsWindow.Show();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
