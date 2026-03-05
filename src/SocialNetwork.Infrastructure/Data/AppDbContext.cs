using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Entities;

namespace SocialNetwork.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<DialogMessage> DialogMessages => Set<DialogMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Post>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Text).IsRequired();
            e.HasIndex(p => p.AuthorId);
            e.HasIndex(p => p.CreatedAt);
            e.HasOne(p => p.Author).WithMany(u => u.Posts).HasForeignKey(p => p.AuthorId);
        });

        modelBuilder.Entity<Friendship>(e =>
        {
            e.HasKey(f => new { f.UserId, f.FriendId });
            e.HasOne(f => f.User).WithMany(u => u.Friends).HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Friend).WithMany().HasForeignKey(f => f.FriendId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(f => f.FriendId);
        });

        modelBuilder.Entity<DialogMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.DialogKey).HasMaxLength(73).IsRequired();
            e.Property(m => m.Text).IsRequired();
            e.HasIndex(m => new { m.DialogKey, m.CreatedAt });
        });
    }
}
