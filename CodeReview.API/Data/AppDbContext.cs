using Microsoft.EntityFrameworkCore;
using CodeReview.API.Models.Entities;

namespace CodeReview.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Each DbSet = one table
    public DbSet<User> Users => Set<User>();
    public DbSet<ReviewJob> ReviewJobs => Set<ReviewJob>();
    public DbSet<ReviewResult> ReviewResults => Set<ReviewResult>();
    public DbSet<ReviewIssue> ReviewIssues => Set<ReviewIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique(); // no duplicate emails
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
        });

        // ReviewJobs table
        modelBuilder.Entity<ReviewJob>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Status).HasDefaultValue("pending");

            // One user has many jobs
            entity.HasOne(j => j.User)
                  .WithMany(u => u.ReviewJobs)
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // delete user = delete their jobs
        });

        // ReviewResults table
        modelBuilder.Entity<ReviewResult>(entity =>
        {
            entity.HasKey(r => r.Id);

            // One job has one result
            entity.HasOne(r => r.Job)
                  .WithOne(j => j.Result)
                  .HasForeignKey<ReviewResult>(r => r.JobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ReviewIssues table
        modelBuilder.Entity<ReviewIssue>(entity =>
        {
            entity.HasKey(i => i.Id);

            // One result has many issues
            entity.HasOne(i => i.Result)
                  .WithMany(r => r.Issues)
                  .HasForeignKey(i => i.ResultId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}