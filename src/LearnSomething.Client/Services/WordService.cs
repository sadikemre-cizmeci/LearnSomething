using LearnSomething.Client.Data;
using LearnSomething.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Services;

// All word CRUD + CSV import goes through here, whether triggered from the UI
// (add/edit/delete a single word) or from a bulk file import. Every word
// belongs to exactly one list.
public class WordService(string dbPath)
{
    private AppDbContext CreateContext() => new(dbPath);

    public async Task<List<WordEntry>> GetByListAsync(Guid listId)
    {
        await using var db = CreateContext();
        return await db.Words.Where(w => w.ListId == listId).OrderBy(w => w.Term).ToListAsync();
    }

    public async Task<WordEntry> AddAsync(Guid listId, string term, string translation, string? example = null, WordStatus status = WordStatus.New)
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var entry = new WordEntry
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            Term = term.Trim(),
            Translation = translation.Trim(),
            Example = string.IsNullOrWhiteSpace(example) ? null : example.Trim(),
            Status = status,
            StatusChangedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Words.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateAsync(Guid id, string term, string translation, string? example, WordStatus status)
    {
        await using var db = CreateContext();
        var entry = await db.Words.FindAsync(id)
            ?? throw new InvalidOperationException($"Kelime bulunamadı: {id}");

        var now = DateTime.UtcNow;
        entry.Term = term.Trim();
        entry.Translation = translation.Trim();
        entry.Example = string.IsNullOrWhiteSpace(example) ? null : example.Trim();

        if (entry.Status != status)
        {
            entry.Status = status;
            entry.StatusChangedAtUtc = now;
        }

        entry.UpdatedAtUtc = now;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = CreateContext();
        var entry = await db.Words.FindAsync(id);
        if (entry is null) return;

        db.Words.Remove(entry);
        await db.SaveChangesAsync();
    }

    // Puts every word in the list back to New, so a list can be restarted
    // from scratch instead of only ever resuming where it was left off.
    public async Task ResetListProgressAsync(Guid listId)
    {
        await using var db = CreateContext();
        var words = await db.Words.Where(w => w.ListId == listId).ToListAsync();
        if (words.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var word in words)
        {
            word.Status = WordStatus.New;
            word.StatusChangedAtUtc = now;
            word.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync();
    }

    // Skips terms that already exist in the same list (case-insensitive) so
    // importing the same file twice into the same list is harmless.
    public async Task<int> ImportFromCsvAsync(Guid listId, string filePath, bool hasHeaderRow)
    {
        var rows = WordCsvImporter.ReadFile(filePath, hasHeaderRow);
        if (rows.Count == 0) return 0;

        await using var db = CreateContext();
        var existingTerms = await db.Words
            .Where(w => w.ListId == listId)
            .Select(w => w.Term.ToLower())
            .ToListAsync();
        var seen = existingTerms.ToHashSet();

        var toAdd = rows.Where(r => seen.Add(r.Term.ToLower())).ToList();
        foreach (var word in toAdd)
        {
            word.ListId = listId;
        }

        db.Words.AddRange(toAdd);
        await db.SaveChangesAsync();
        return toAdd.Count;
    }
}
