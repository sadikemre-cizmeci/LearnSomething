namespace LearnSomething.Shared;

public class WordEntry
{
    public Guid Id { get; set; }

    public Guid ListId { get; set; }

    public required string Term { get; set; }

    public required string Translation { get; set; }

    public string? Example { get; set; }

    public WordStatus Status { get; set; } = WordStatus.New;

    // When Status last changed. Used to keep a word just marked ToRepeat out of
    // today's repeat queue (it should only resurface on a later day).
    public DateTime StatusChangedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
