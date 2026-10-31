using HireSync.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HireSync.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AtsScanLog> AtsScanLogs { get; set; }
        public DbSet<InterviewLog> InterviewLogs { get; set; }
        public DbSet<User> Users { get; set; }
    }
}