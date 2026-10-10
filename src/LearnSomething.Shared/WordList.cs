namespace LearnSomething.Shared;

public class WordList
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    // true: card shows Term first, reveals Translation. false: reversed.
    // Per-list so different lists (e.g. different language pairs) can each
    // have their own direction.
    public bool ShowTermFirst { get; set; } = true;

    // Which words this list studies from when it's the active list.
    public StudyMode StudyMode { get; set; } = StudyMode.NewAndReview;

    // Daily quota tracking, kept here (not in-memory) so the count survives
    // closing the card, or the whole app, and reopening later the same day.
    // DailyProgressDate is the UTC date the two fields below apply to; when
    // it's stale (not today), both are lazily reset the next time anything
    // reads or writes this list's daily state — no separate midnight job.
    public DateTime? DailyProgressDate { get; set; }

    public int WordsShownToday { get; set; }

    // Once true (set when the user clicks "Devam et"), today's daily cap is
    // bypassed entirely for this list until the date rolls over.
    public bool ExceededQuotaToday { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
