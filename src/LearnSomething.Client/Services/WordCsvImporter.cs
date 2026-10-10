using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using LearnSomething.Client.Localization;
using LearnSomething.Shared;

namespace LearnSomething.Client.Services;

// Import file format: plain CSV, matched by column POSITION, not header
// text — column 1 is the word, column 2 its meaning, column 3 an optional
// example sentence. Whether row 1 is a header to discard or real data is
// genuinely ambiguous from the file alone (a header row and a two-word
// data row look identical), so the caller says which — there's no reliable
// way to guess this without risking silently dropping someone's first word.
// If the word or meaning column is empty on any data row, the whole import
// is rejected (not silently skipped), so the user finds out exactly which
// row needs fixing instead of getting a partial, unexplained import.
public static class WordCsvImporter
{
    public static List<WordEntry> ReadFile(string filePath, bool hasHeaderRow)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            // Example is optional — a row (or the whole file) with only 2
            // columns must not throw just because the 3rd field is absent.
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        var now = DateTime.UtcNow;
        var result = new List<WordEntry>();
        var rowNumber = 0;

        while (csv.Read())
        {
            rowNumber++;

            if (rowNumber == 1 && hasHeaderRow)
            {
                continue;
            }

            var term = csv.GetField(0)?.Trim() ?? string.Empty;
            if (term.Length == 0)
            {
                throw new InvalidDataException(string.Format(Strings.Instance.CsvMissingTermFormat, rowNumber));
            }

            var translation = csv.GetField(1)?.Trim() ?? string.Empty;
            if (translation.Length == 0)
            {
                throw new InvalidDataException(string.Format(Strings.Instance.CsvMissingTranslationFormat, rowNumber));
            }

            var example = csv.GetField(2)?.Trim();

            result.Add(new WordEntry
            {
                Id = Guid.NewGuid(),
                Term = term,
                Translation = translation,
                Example = string.IsNullOrWhiteSpace(example) ? null : example,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        return result;
    }
}
