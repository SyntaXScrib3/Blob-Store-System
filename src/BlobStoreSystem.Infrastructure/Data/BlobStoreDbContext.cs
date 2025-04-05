using Microsoft.EntityFrameworkCore;
using BlobStoreSystem.Domain.Entities;

namespace BlobStoreSystem.Infrastructure.Data;

public class BlobStoreDbContext : DbContext
{
    public DbSet<DirectoryNode> Directories { get; set; } = null!;
    public DbSet<FileNode> Files { get; set; } = null!;
    public DbSet<Blob> Blobs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public BlobStoreDbContext(DbContextOptions<BlobStoreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DirectoryNode
        modelBuilder.Entity<DirectoryNode>(entity =>
        {
            entity.ToTable("Directories");
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.ParentDirectory)
                  .WithMany()
                  .HasForeignKey(d => d.ParentDirectoryId);
        });

        // FileNode
        modelBuilder.Entity<FileNode>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(f => f.Id);
            entity.HasOne(f => f.ParentDirectory)
                  .WithMany()
                  .HasForeignKey(f => f.ParentDirectoryId);
        });

        // Blob
        modelBuilder.Entity<Blob>(entity =>
        {
            entity.ToTable("Blobs");
            entity.HasKey(b => b.Id);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }
}
