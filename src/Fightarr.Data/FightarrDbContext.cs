using Microsoft.EntityFrameworkCore;
using Fightarr.Core.Models;
using System.Text.Json;

namespace Fightarr.Data;

public class FightarrDbContext : DbContext
{
    public FightarrDbContext(DbContextOptions<FightarrDbContext> options) : base(options) { }
    
    public DbSet<FightEvent> FightEvents { get; set; }
    public DbSet<Fight> Fights { get; set; }
    public DbSet<EventFile> EventFiles { get; set; }
    public DbSet<QualityProfile> QualityProfiles { get; set; }
    public DbSet<Quality> Qualities { get; set; }
    public DbSet<DownloadClient> DownloadClients { get; set; }
    public DbSet<Indexer> Indexers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // FightEvent configuration
        modelBuilder.Entity<FightEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.EventDate);
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Promotion).IsRequired().HasMaxLength(100);
            
            entity.HasMany(e => e.Fights)
                  .WithOne(f => f.FightEvent)
                  .HasForeignKey(f => f.FightEventId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany(e => e.EventFiles)
                  .WithOne(f => f.FightEvent)
                  .HasForeignKey(f => f.FightEventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Fight configuration
        modelBuilder.Entity<Fight>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Fighters)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                  );
        });
        
        // EventFile configuration
        modelBuilder.Entity<EventFile>(entity =>
        {
            entity.HasKey(ef => ef.Id);
            entity.Property(ef => ef.Path).IsRequired().HasMaxLength(1000);
            entity.Property(ef => ef.Languages)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                  );
                  
            entity.HasOne(ef => ef.Quality)
                  .WithMany()
                  .HasForeignKey(ef => ef.QualityId);
        });
        
        // QualityProfile configuration
        modelBuilder.Entity<QualityProfile>(entity =>
        {
            entity.HasKey(qp => qp.Id);
            entity.Property(qp => qp.Name).IsRequired().HasMaxLength(200);
            entity.Property(qp => qp.Items)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<QualityItem>>(v, (JsonSerializerOptions?)null) ?? new List<QualityItem>()
                  );
        });
        
        // Quality configuration
        modelBuilder.Entity<Quality>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Name).IsRequired().HasMaxLength(100);
            entity.Property(q => q.Source).HasMaxLength(100);
        });
        
        // DownloadClient configuration
        modelBuilder.Entity<DownloadClient>(entity =>
        {
            entity.HasKey(dc => dc.Id);
            entity.Property(dc => dc.Name).IsRequired().HasMaxLength(200);
            entity.Property(dc => dc.Host).IsRequired().HasMaxLength(255);
        });
        
        // Indexer configuration
        modelBuilder.Entity<Indexer>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Name).IsRequired().HasMaxLength(200);
            entity.Property(i => i.BaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(i => i.Categories)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                  );
        });
        
        // Seed default data
        SeedDefaultData(modelBuilder);
    }
    
    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed default qualities
        modelBuilder.Entity<Quality>().HasData(
            new Quality { Id = 1, Name = "4K", Source = "WEB-DL", Resolution = 2160 },
            new Quality { Id = 2, Name = "1080p", Source = "WEB-DL", Resolution = 1080 },
            new Quality { Id = 3, Name = "720p", Source = "WEB-DL", Resolution = 720 },
            new Quality { Id = 4, Name = "480p", Source = "WEB-DL", Resolution = 480 },
            new Quality { Id = 5, Name = "Unknown", Source = "Unknown", Resolution = 0 }
        );
        
        // Seed default quality profiles
        modelBuilder.Entity<QualityProfile>().HasData(
            new QualityProfile 
            { 
                Id = 1, 
                Name = "HD - 1080p/720p", 
                Cutoff = 2,
                Language = "English"
            },
            new QualityProfile 
            { 
                Id = 2, 
                Name = "Ultra-HD", 
                Cutoff = 1,
                Language = "English"
            },
            new QualityProfile 
            { 
                Id = 3, 
                Name = "Standard Definition", 
                Cutoff = 4,
                Language = "English"
            }
        );
    }
}
