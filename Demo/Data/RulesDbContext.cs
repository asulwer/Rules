using Microsoft.EntityFrameworkCore;
using RoslynRules.Models;

namespace Demo.Data;

public class RulesDbContext : DbContext
{
    public DbSet<Workflow> Workflows { get; set; } = null!;
    public DbSet<Rule> Rules { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("RulesDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Workflow configuration
        modelBuilder.Entity<Workflow>().HasKey(w => w.Id);
        modelBuilder.Entity<Workflow>()
            .HasMany(w => w.Rules)
            .WithOne()
            .HasForeignKey(r => r.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rule configuration
        modelBuilder.Entity<Rule>().HasKey(r => r.Id);
        modelBuilder.Entity<Rule>()
            .HasMany(r => r.ChildRules)
            .WithOne()
            .HasForeignKey(r => r.ParentRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore compiled delegate fields (runtime-only, not persisted)
        modelBuilder.Entity<Rule>().Ignore("CompiledExpression");
        modelBuilder.Entity<Rule>().Ignore("CompiledAction");
        modelBuilder.Entity<Rule>().Ignore("ResultCache");
    }
}
