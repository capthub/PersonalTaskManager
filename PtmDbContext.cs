using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace PersonalTaskManager
{
    public class PtmDbContext : DbContext
    {
        public DbSet<TaskModel> Tasks { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=PtmDb.db").UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskModel>().ToTable("Tasks");
        }
    }
}
