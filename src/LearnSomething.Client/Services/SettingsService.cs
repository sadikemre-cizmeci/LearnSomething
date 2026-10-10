using LearnSomething.Client.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Services;

public class SettingsService(string dbPath)
{
    private AppDbContext CreateContext() => new(dbPath);

    public async Task<AppSettingsEntity> GetAsync()
    {
        await using var db = CreateContext();
        var settings = await db.Settings.FindAsync(1);
        if (settings is not null) return settings;

        settings = new AppSettingsEntity();
        db.Settings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    public async Task UpdateAsync(Action<AppSettingsEntity> apply)
    {
        await using var db = CreateContext();
        var settings = await db.Settings.FindAsync(1);
        if (settings is null)
        {
            settings = new AppSettingsEntity();
            db.Settings.Add(settings);
        }

        apply(settings);
        await db.SaveChangesAsync();
    }

    public async Task SaveCardBoundsAsync(double x, double y, double width, double height)
    {
        await UpdateAsync(s =>
        {
            s.CardX = x;
            s.CardY = y;
            s.CardWidth = width;
            s.CardHeight = height;
        });
    }

    public async Task ResetCardBoundsAsync()
    {
        await UpdateAsync(s =>
        {
            s.CardX = null;
            s.CardY = null;
            s.CardWidth = null;
            s.CardHeight = null;
        });
    }
}
