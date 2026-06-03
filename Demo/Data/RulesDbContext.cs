using Microsoft.EntityFrameworkCore;
using RoslynRules.Models;
using System.Text.Json;

namespace Demo.Data;

public class RulesDbContext : DbContext
{
    public DbSet<WorkflowEntity> Workflows { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("RulesDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowEntity>().HasKey(w => w.Id);
    }
}

public class WorkflowEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string JsonPayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
