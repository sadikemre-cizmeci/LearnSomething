using System.Diagnostics;

namespace LearnSomething.Client.Services;

// Registers/unregisters the app to launch automatically when the user logs in.
// macOS: a LaunchAgent plist under ~/Library/LaunchAgents.
// Windows: a value under HKCU\...\CurrentVersion\Run.
public class StartupService
{
    private const string AppName = "LearnSomething";
    private const string MacLaunchAgentLabel = "com.learnsomething.client";
    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled()
    {
        if (OperatingSystem.IsMacOS())
            return File.Exists(MacPlistPath);

        if (OperatingSystem.IsWindows())
            return GetWindowsRunValue() is not null;

        return false;
    }

    public void SetEnabled(bool enabled)
    {
        if (OperatingSystem.IsMacOS())
        {
            if (enabled) EnableMac();
            else DisableMac();
        }
        else if (OperatingSystem.IsWindows())
        {
            if (enabled) EnableWindows();
            else DisableWindows();
        }
    }

    private static string MacPlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{MacLaunchAgentLabel}.plist");

    private static void EnableMac()
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Uygulamanın çalıştırılabilir dosya yolu bulunamadı.");

        var plist = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{MacLaunchAgentLabel}</string>
                <key>ProgramArguments</key>
                <array>
                    <string>{exePath}</string>
                    <string>--autostart</string>
                </array>
                <key>RunAtLoad</key>
                <true/>
            </dict>
            </plist>
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(MacPlistPath)!);
        File.WriteAllText(MacPlistPath, plist);
        RunProcess("launchctl", $"load -w \"{MacPlistPath}\"");
    }

    private static void DisableMac()
    {
        if (!File.Exists(MacPlistPath)) return;

        RunProcess("launchctl", $"unload -w \"{MacPlistPath}\"");
        File.Delete(MacPlistPath);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void EnableWindows()
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Uygulamanın çalıştırılabilir dosya yolu bulunamadı.");

        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(WindowsRunKeyPath);
        key.SetValue(AppName, $"\"{exePath}\" --autostart");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void DisableWindows()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(WindowsRunKeyPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? GetWindowsRunValue()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(WindowsRunKeyPath);
        return key?.GetValue(AppName) as string;
    }

    private static void RunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            process?.WaitForExit(5000);
        }
        catch
        {
            // Best-effort: the plist file is the source of truth — if launchctl can't
            // be reached right now, login items still pick it up on next boot.
        }
    }
}
