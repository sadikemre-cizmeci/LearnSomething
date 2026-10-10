using LearnSomething.Client.Services;

namespace LearnSomething.Client.Data;

public static class DatabaseSeeder
{
    // Only seeds when there isn't a single list yet, so it never runs again
    // once the user has created or imported anything of their own.
    public static async Task EnsureSeededAsync(WordListService listService, WordService wordService)
    {
        var lists = await listService.GetAllAsync();
        if (lists.Count > 0) return;

        var sampleFile = Path.Combine(AppContext.BaseDirectory, "SampleData", "words-sample.csv");
        if (!File.Exists(sampleFile)) return;

        var list = await listService.CreateAsync("Örnek Kelimeler");
        await wordService.ImportFromCsvAsync(list.Id, sampleFile, hasHeaderRow: true);
    }
}
