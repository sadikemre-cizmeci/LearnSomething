using LearnSomething.Client.Data;
using LearnSomething.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Services;

// HasProgress: true once any word in the list has moved past New — used to
// decide whether "restart from scratch" is even a meaningful action to offer.
public record WordListSummary(Guid Id, string Name, int WordCount, bool HasProgress);

public class WordListService(string dbPath)
{
    private AppDbContext CreateContext() => new(dbPath);

    public async Task<List<WordListSummary>> GetAllAsync()
    {
        await using var db = CreateContext();

        return await db.Lists
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => new WordListSummary(
                l.Id,
                l.Name,
                db.Words.Count(w => w.ListId == l.Id),
                db.Words.Any(w => w.ListId == l.Id && w.Status != WordStatus.New)))
            .ToListAsync();
    }

    public async Task<WordList?> GetByIdAsync(Guid id)
    {
        await using var db = CreateContext();
        return await db.Lists.FindAsync(id);
    }

    // Full entities (StudyMode, ShowTermFirst included), unlike GetAllAsync's
    // lightweight summaries — used when every list needs to be studied at
    // once, each per its own settings.
    public async Task<List<WordList>> GetAllListsAsync()
    {
        await using var db = CreateContext();
        return await db.Lists.OrderBy(l => l.CreatedAtUtc).ToListAsync();
    }

    public async Task SetShowTermFirstAsync(Guid id, bool showTermFirst)
    {
        await using var db = CreateContext();
        var list = await db.Lists.FindAsync(id);
        if (list is null) return;

        list.ShowTermFirst = showTermFirst;
        await db.SaveChangesAsync();
    }

    public async Task SetStudyModeAsync(Guid id, StudyMode mode)
    {
        await using var db = CreateContext();
        var list = await db.Lists.FindAsync(id);
        if (list is null) return;

        list.StudyMode = mode;
        await db.SaveChangesAsync();
    }

    // Lazily rolls a list's daily-quota fields over to today when they're
    // stale — the effective reset mechanism, since there's no midnight job.
    private static bool RolloverIfNewDay(WordList list, DateTime today)
    {
        if (list.DailyProgressDate == today) return false;

        list.DailyProgressDate = today;
        list.WordsShownToday = 0;
        list.ExceededQuotaToday = false;
        return true;
    }

    // Reads (rolling over to today first if needed) how many words this
    // list has shown today and whether its quota's already been exceeded.
    public async Task<(int WordsShownToday, bool ExceededQuotaToday)> GetDailyProgressAsync(Guid listId)
    {
        await using var db = CreateContext();
        var list = await db.Lists.FindAsync(listId);
        if (list is null) return (0, false);

        if (RolloverIfNewDay(list, DateTime.UtcNow.Date))
        {
            await db.SaveChangesAsync();
        }

        return (list.WordsShownToday, list.ExceededQuotaToday);
    }

    // Called once per word actually shown on the card (not merely queued).
    public async Task RecordWordShownAsync(Guid listId)
    {
        await using var db = CreateContext();
        var list = await db.Lists.FindAsync(listId);
        if (list is null) return;

        RolloverIfNewDay(list, DateTime.UtcNow.Date);
        list.WordsShownToday++;
        await db.SaveChangesAsync();
    }

    // Called when the user clicks "Devam et" — bypasses today's cap for
    // this list entirely, for the rest of the calendar day, surviving the
    // card (or the whole app) being closed and reopened later that day.
    public async Task MarkQuotaExceededTodayAsync(Guid listId)
    {
        await using var db = CreateContext();
        var list = await db.Lists.FindAsync(listId);
        if (list is null) return;

        RolloverIfNewDay(list, DateTime.UtcNow.Date);
        list.ExceededQuotaToday = true;
        await db.SaveChangesAsync();
    }

    public async Task<WordList> CreateAsync(string name)
    {
        await using var db = CreateContext();

        var list = new WordList { Id = Guid.NewGuid(), Name = name.Trim(), CreatedAtUtc = DateTime.UtcNow };
        db.Lists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    public async Task RenameAsync(Guid id, string newName)
    {
        await using var db = CreateContext();

        var list = await db.Lists.FindAsync(id);
        if (list is null) return;

        list.Name = newName.Trim();
        await db.SaveChangesAsync();
    }

    // Cascade-deletes every word in the list too (configured at the DB level).
    public async Task DeleteAsync(Guid id)
    {
        await using var db = CreateContext();

        var list = await db.Lists.FindAsync(id);
        if (list is null) return;

        db.Lists.Remove(list);
        await db.SaveChangesAsync();
    }
}
