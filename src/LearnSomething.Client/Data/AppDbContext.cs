using LearnSomething.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnSomething.Client.Data;

public class AppDbContext(string dbPath) : DbContext
{
    public DbSet<WordEntry> Words => Set<WordEntry>();

    public DbSet<WordList> Lists => Set<WordList>();

    public DbSet<AppSettingsEntity> Settings => Set<AppSettingsEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WordEntry>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Term).IsRequired();
            entity.Property(w => w.Translation).IsRequired();
            entity.HasIndex(w => w.Term);
            entity.HasIndex(w => w.Status);
            entity.HasIndex(w => w.ListId);
            entity.HasOne<WordList>()
                .WithMany()
                .HasForeignKey(w => w.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WordList>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Name).IsRequired();
            entity.Property(l => l.ShowTermFirst).HasDefaultValue(true);
        });

        modelBuilder.Entity<AppSettingsEntity>(entity =>
        {
            entity.HasKey(s => s.Id);
        });
    }
}
