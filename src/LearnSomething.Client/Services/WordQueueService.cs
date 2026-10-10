using LearnSomething.Client.Data;
using LearnSomething.Client.Localization;
using LearnSomething.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Services;

// AllFinished is true only once every list in scope (the active list, or
// every list when StudyAllLists is on) has truly run out of words for its
// own study mode — not just "today's quota is full, more exists for later".
// Message is a "\n"-joined line per finished list, or null when not all
// finished (the regular daily-quota-reached flow applies instead).
public record CompletionStatus(bool AllFinished, string? Message);

public record DailyQueue(List<WordEntry> NewWords, List<WordEntry> RepeatWords, List<WordEntry> LearnedWords)
{
    public List<WordEntry> All => [.. NewWords, .. RepeatWords, .. LearnedWords];
}

// Builds and drives today's card sequence: which new words, previously
// "tekrar et"-marked words, or already-learned words are due, from the
// currently active list, respecting that list's own study mode.
public class WordQueueService(string dbPath, SettingsService settingsService, WordListService listService)
{
    private AppDbContext CreateContext() => new(dbPath);

    // The active list, or every list when StudyAllLists is on — shared by
    // every method below that needs to know which lists are "in play".
    private async Task<List<WordList>> GetListsInScopeAsync()
    {
        var settings = await settingsService.GetAsync();

        if (settings.StudyAllLists)
        {
            return await listService.GetAllListsAsync();
        }

        if (settings.ActiveListId is not { } listId)
        {
            return [];
        }

        var activeList = await listService.GetByIdAsync(listId);
        return activeList is null ? [] : [activeList];
    }

    public async Task<DailyQueue> BuildTodaysQueueAsync()
    {
        var settings = await settingsService.GetAsync();
        var dailyCount = settings.DailyWordCount;
        var listsInScope = await GetListsInScopeAsync();

        var newWords = new List<WordEntry>();
        var repeatWords = new List<WordEntry>();
        var learnedWords = new List<WordEntry>();

        foreach (var list in listsInScope)
        {
            var queue = await BuildQueueForListAsync(list, dailyCount);
            newWords.AddRange(queue.NewWords);
            repeatWords.AddRange(queue.RepeatWords);
            learnedWords.AddRange(queue.LearnedWords);
        }

        return new DailyQueue(newWords, repeatWords, learnedWords);
    }

    private async Task<DailyQueue> BuildQueueForListAsync(WordList list, int dailyCount)
    {
        var mode = list.StudyMode;
        var today = DateTime.UtcNow.Date;

        // Reviewing what's already learned is a deliberate, on-demand session,
        // not something to trickle in behind a daily cap — show everything.
        if (mode == StudyMode.LearnedOnly)
        {
            await using var learnedDb = CreateContext();
            var learnedWords = await learnedDb.Words
                .Where(w => w.ListId == list.Id && w.Status == WordStatus.Learned)
                .OrderBy(w => w.StatusChangedAtUtc)
                .ToListAsync();
            return new DailyQueue([], [], learnedWords);
        }

        // Today's remaining budget for this specific list — not the raw
        // dailyCount setting, since part (or all) of it may already have
        // been used up in an earlier session today. Null once the user has
        // clicked "Devam et" for this list today: no cap at all until
        // tomorrow.
        var (wordsShownToday, exceededQuotaToday) = await listService.GetDailyProgressAsync(list.Id);
        int? effectiveCap = exceededQuotaToday ? null : Math.Max(0, dailyCount - wordsShownToday);

        await using var db = CreateContext();

        // Marked-for-repeat words never resurface the same day they were marked.
        var eligibleRepeat = mode is StudyMode.ReviewOnly or StudyMode.NewAndReview
            ? (await db.Words
                    .Where(w => w.ListId == list.Id && w.Status == WordStatus.ToRepeat)
                    .OrderBy(w => w.StatusChangedAtUtc)
                    .ToListAsync())
                .Where(w => w.StatusChangedAtUtc.Date < today)
                .ToList()
            : [];

        if (mode == StudyMode.ReviewOnly)
        {
            var repeatOnly = effectiveCap is { } cap ? eligibleRepeat.Take(cap).ToList() : eligibleRepeat;
            return new DailyQueue([], repeatOnly, []);
        }

        if (mode == StudyMode.NewOnly)
        {
            var newQuery = db.Words
                .Where(w => w.ListId == list.Id && w.Status == WordStatus.New)
                .OrderBy(w => w.CreatedAtUtc);
            var onlyNew = effectiveCap is { } cap
                ? await newQuery.Take(cap).ToListAsync()
                : await newQuery.ToListAsync();
            return new DailyQueue(onlyNew, [], []);
        }

        // NewAndReview: one shared daily budget — due reviews first, then fill
        // whatever's left with new words, rather than two separate caps.
        List<WordEntry> repeatWords;
        List<WordEntry> newWords;

        if (effectiveCap is { } sharedCap)
        {
            repeatWords = eligibleRepeat.Take(sharedCap).ToList();
            var remaining = sharedCap - repeatWords.Count;
            newWords = remaining > 0
                ? await db.Words
                    .Where(w => w.ListId == list.Id && w.Status == WordStatus.New)
                    .OrderBy(w => w.CreatedAtUtc)
                    .Take(remaining)
                    .ToListAsync()
                : [];
        }
        else
        {
            repeatWords = eligibleRepeat;
            newWords = await db.Words
                .Where(w => w.ListId == list.Id && w.Status == WordStatus.New)
                .OrderBy(w => w.CreatedAtUtc)
                .ToListAsync();
        }

        return new DailyQueue(newWords, repeatWords, []);
    }

    // Called once per word actually shown on the card (not merely queued),
    // so reopening the app later the same day picks up from the real count.
    public Task RecordWordShownAsync(Guid listId) => listService.RecordWordShownAsync(listId);

    // Called when the user presses "Devam et" — marks every list currently
    // in scope as having exceeded today's quota, so it stays uncapped for
    // the rest of the day even across closing and reopening the app.
    public async Task MarkQuotaExceededForScopeAsync()
    {
        foreach (var list in await GetListsInScopeAsync())
        {
            await listService.MarkQuotaExceededTodayAsync(list.Id);
        }
    }

    // Used when the user presses "Devam et" to keep going past today's quota.
    public async Task<List<WordEntry>> GetMoreNewWordsAsync(IEnumerable<Guid> alreadyShownIds, int count = 5)
    {
        var exclude = alreadyShownIds.ToHashSet();
        var listsInScope = await GetListsInScopeAsync();

        await using var db = CreateContext();
        var result = new List<WordEntry>();
        foreach (var list in listsInScope)
        {
            result.AddRange(await GetMoreNewWordsForListAsync(db, list, exclude, count));
        }

        return result;
    }

    private static Task<List<WordEntry>> GetMoreNewWordsForListAsync(AppDbContext db, WordList list, HashSet<Guid> exclude, int count)
    {
        if (list.StudyMode is not (StudyMode.NewOnly or StudyMode.NewAndReview))
        {
            return Task.FromResult(new List<WordEntry>());
        }

        return db.Words
            .Where(w => w.ListId == list.Id && w.Status == WordStatus.New && !exclude.Contains(w.Id))
            .OrderBy(w => w.CreatedAtUtc)
            .Take(count)
            .ToListAsync();
    }

    // Checked whenever the card's queue runs dry, to tell "today's cap is
    // full, come back tomorrow" apart from "there's genuinely nothing left
    // to study in this mode" — the latter needs every list currently in
    // scope to be exhausted for its own mode, not just the one that just ran out.
    public async Task<CompletionStatus> CheckCompletionAsync()
    {
        var listsInScope = await GetListsInScopeAsync();
        if (listsInScope.Count == 0)
        {
            return new CompletionStatus(false, null);
        }

        await using var db = CreateContext();
        var finishedLines = new List<string>();

        foreach (var list in listsInScope)
        {
            var newCount = await db.Words.CountAsync(w => w.ListId == list.Id && w.Status == WordStatus.New);
            var toRepeatCount = await db.Words.CountAsync(w => w.ListId == list.Id && w.Status == WordStatus.ToRepeat);
            var learnedCount = await db.Words.CountAsync(w => w.ListId == list.Id && w.Status == WordStatus.Learned);

            var isFinished = list.StudyMode switch
            {
                StudyMode.NewOnly => newCount == 0,
                StudyMode.ReviewOnly => toRepeatCount == 0,
                StudyMode.NewAndReview => newCount == 0 && toRepeatCount == 0,
                StudyMode.LearnedOnly => learnedCount == 0,
                _ => false,
            };

            if (!isFinished)
            {
                return new CompletionStatus(false, null);
            }

            finishedLines.Add(string.Format(Strings.Instance.ListFinishedLineFormat, list.Name, ModeLabel(list.StudyMode)));
        }

        return new CompletionStatus(true, string.Join("\n", finishedLines));
    }

    private static string ModeLabel(StudyMode mode) => mode switch
    {
        StudyMode.NewOnly => Strings.Instance.StudyModeNewOnly,
        StudyMode.ReviewOnly => Strings.Instance.StudyModeReviewOnly,
        StudyMode.LearnedOnly => Strings.Instance.StudyModeLearnedOnly,
        _ => Strings.Instance.StudyModeNewAndReview,
    };

    public Task MarkLearnedAsync(Guid id) => SetStatusAsync(id, WordStatus.Learned);

    public Task MarkForRepeatAsync(Guid id) => SetStatusAsync(id, WordStatus.ToRepeat);

    private async Task SetStatusAsync(Guid id, WordStatus status)
    {
        await using var db = CreateContext();
        var word = await db.Words.FindAsync(id);
        if (word is null) return;

        var now = DateTime.UtcNow;
        word.Status = status;
        word.StatusChangedAtUtc = now;
        word.UpdatedAtUtc = now;
        await db.SaveChangesAsync();
    }
}
