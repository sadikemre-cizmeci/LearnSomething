using LearnSomething.Client.Localization;
using LearnSomething.Client.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Data;

public static class AppStartup
{
    public static async Task<AppServices> InitializeAsync()
    {
        var dbPath = AppPaths.GetDatabaseFilePath();

        bool isFirstEverRun;
        await using (var db = new AppDbContext(dbPath))
        {
            await db.Database.MigrateAsync();
            isFirstEverRun = !await db.Settings.AnyAsync();
        }

        var wordService = new WordService(dbPath);
        var listService = new WordListService(dbPath);
        await DatabaseSeeder.EnsureSeededAsync(listService, wordService);

        var settingsService = new SettingsService(dbPath);
        var startupService = new StartupService();
        var settings = await settingsService.GetAsync();
        Strings.Instance.CurrentLanguage = settings.Language;

        if (isFirstEverRun)
        {
            // "Bilgisayar açılınca başlat" should feel on from the start —
            // that's the whole premise of the app, not an opt-in extra.
            startupService.SetEnabled(true);
        }

        // If nothing has ever been picked as "active" yet, default to the first
        // available list so the card works out of the box.
        if (settings.ActiveListId is null)
        {
            var lists = await listService.GetAllAsync();
            if (lists.Count > 0)
            {
                var firstListId = lists[0].Id;
                await settingsService.UpdateAsync(s => s.ActiveListId = firstListId);
                settings = await settingsService.GetAsync();
            }
        }

        var queueService = new WordQueueService(dbPath, settingsService, listService);

        return new AppServices(wordService, listService, settingsService, queueService, startupService, settings);
    }
}
