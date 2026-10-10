using LearnSomething.Shared;

namespace LearnSomething.Client.Data;

// Single-row table (Id is always 1) holding the user's local preferences.
public class AppSettingsEntity
{
    public int Id { get; set; } = 1;

    // Total words (new + due-for-review combined) the card shows per day.
    // Deliberately low default: the app should feel optional, never like a chore.
    public int DailyWordCount { get; set; } = 1;

    // Which list the card currently studies from. Null only before the user
    // (or the first-run seeding) has ever picked one. Ignored when
    // StudyAllLists is on — every list participates instead of just this one.
    public Guid? ActiveListId { get; set; }

    // When true, DailyWordCount is applied per list (each list's own study
    // mode still governs what it individually contributes) instead of to a
    // single active list.
    public bool StudyAllLists { get; set; }

    public bool StartOnBoot { get; set; }

    // When true, reaching today's quota (with more words genuinely left for
    // later) closes the card — and the whole app, if nothing else is open —
    // instead of showing "Hedefine ulaştın" with a "Devam et" option.
    // Deliberately does not apply once a list has truly run out of words for
    // its mode (IsAllFinished) — that state still shows its own message.
    public bool CloseCardWhenQuotaReached { get; set; }

    public AppLanguage Language { get; set; } = AppLanguage.Turkish;

    public double? CardX { get; set; }

    public double? CardY { get; set; }

    public double? CardWidth { get; set; }

    public double? CardHeight { get; set; }
}
