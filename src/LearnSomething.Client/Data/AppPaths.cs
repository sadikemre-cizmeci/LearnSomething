namespace LearnSomething.Client.Data;

public static class AppPaths
{
    private const string AppFolderName = "LearnSomething";

    public static string GetAppDataDirectory()
    {
        var baseDirectory = OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support")
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var directory = Path.Combine(baseDirectory, AppFolderName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetDatabaseFilePath() => Path.Combine(GetAppDataDirectory(), "learnsomething.db");
}
