using Microsoft.EntityFrameworkCore;
using IssueTrackerApi.Models;

namespace IssueTrackerApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Issue> Issues => Set<Issue>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<IssueHistory> IssueHistories => Set<IssueHistory>();
    }
}