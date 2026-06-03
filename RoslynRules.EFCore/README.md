# RoslynRules.EFCore

EF Core integration for RoslynRules. Provides entity mapping and lazy loading support while keeping the core `Rule` model sealed and immutable.

## Problem

`Rule` is `sealed` to enforce immutability after compilation. This prevents EF Core lazy loading proxies from working (proxies require subclassing).

## Solution

This package provides **separate EF entities** (`RuleEntity`, `WorkflowEntity`) that map to the same database schema. You use EF's entities for persistence and lazy loading, then convert to sealed domain models for execution.

## Usage

### 1. Configure your DbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<WorkflowEntity> Workflows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureRoslynRules();
    }
}
```

### 2. Load with lazy loading, convert to domain model

```csharp
// Lazy loading works — RuleEntity is NOT sealed
var entity = await db.Workflows.FirstAsync();

// Convert to sealed domain model
var workflow = entity.ToDomainModel();

// Compile and execute as usual
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
var results = workflow.Execute(new[] { new RuleParameter("customer", typeof(Customer), customer) });
```

## How It Works

| Layer | Type | Sealed | Lazy Loading |
|-------|------|--------|-------------|
| `RuleEntity` | EF persistence | No | Yes |
| `Rule` | Domain/execution | Yes | No |

The `ToDomainModel()` method recursively converts the entity graph to sealed `Rule` instances. Once converted, the domain model has full immutability and compilation safety.
