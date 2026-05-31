---
layout: default
title: EF Core Serialization
parent: Examples
nav_order: 9
---

[← Back to Examples Index](index.md)

# EF Core Serialization

Store and load rules from a database using Entity Framework Core.

## Table of Contents
- [Database Schema](#database-schema)
- [DbContext Setup](#dbcontext-setup)
- [Storing Rules](#storing-rules)
- [Loading Rules](#loading-rules)
- [JSON Configuration Column](#json-configuration-column)
- [Rule Versioning with Temporal Tables](#rule-versioning-with-temporal-tables)
- [Querying Rules](#querying-rules)
- [Performance Considerations](#performance-considerations)

## Database Schema

### Basic Tables

```sql
-- Workflows table
CREATE TABLE Workflows (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Description NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Rules table
CREATE TABLE Rules (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Description NVARCHAR(500),
    Expression NVARCHAR(MAX),
    [Action] NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1,
    Priority INT NOT NULL DEFAULT 0,
    DependsOnRuleId UNIQUEIDENTIFIER NULL,
    WorkflowId UNIQUEIDENTIFIER NOT NULL,
    ParentRuleId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Rules_Workflows FOREIGN KEY (WorkflowId) REFERENCES Workflows(Id),
    CONSTRAINT FK_Rules_Parent FOREIGN KEY (ParentRuleId) REFERENCES Rules(Id),
    CONSTRAINT FK_Rules_Dependency FOREIGN KEY (DependsOnRuleId) REFERENCES Rules(Id)
);
```

## DbContext Setup

```csharp
using Microsoft.EntityFrameworkCore;
using Rules.Models;

public class RulesDbContext : DbContext
{
    public DbSet<Workflow> Workflows { get; set; } = null!;
    public DbSet<Rule> Rules { get; set; } = null!;

    public RulesDbContext(DbContextOptions<RulesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Workflow configuration
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            
            entity.HasMany(e => e.Rules)
                .WithOne(e => e.Workflow)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Rule configuration
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Expression).HasColumnType("NVARCHAR(MAX)");
            entity.Property(e => e.Action).HasColumnType("NVARCHAR(MAX)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            
            // Self-referencing for parent-child
            entity.HasOne(e => e.ParentRule)
                .WithMany(e => e.ChildRules)
                .HasForeignKey(e => e.ParentRuleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Dependency relationship
            entity.HasOne(e => e.DependsOnRule)
                .WithMany()
                .HasForeignKey(e => e.DependsOnRuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

## Storing Rules

### Creating a Workflow with Rules

```csharp
public async Task<Guid> CreateWorkflowAsync(Workflow workflow)
{
    using var context = new RulesDbContext(_options);
    
    // Ensure all rules have IDs
    foreach (var rule in workflow.Rules)
    {
        if (rule.Id == Guid.Empty)
            rule.Id = Guid.NewGuid();
    }
    
    // Resolve dependencies before saving
    foreach (var rule in workflow.Rules.Where(r => r.DependsOnRuleId.HasValue))
    {
        var depRule = workflow.Rules.FirstOrDefault(r => r.Id == rule.DependsOnRuleId.Value);
        if (depRule != null)
        {
            rule.DependsOnRule = depRule;
        }
    }
    
    context.Workflows.Add(workflow);
    await context.SaveChangesAsync();
    
    return workflow.Id;
}
```

### Updating Rules

```csharp
public async Task UpdateRuleAsync(Guid ruleId, string newExpression)
{
    using var context = new RulesDbContext(_options);
    
    var rule = await context.Rules.FindAsync(ruleId);
    if (rule == null) throw new KeyNotFoundException("Rule not found");
    
    // Note: After compilation, rules are immutable.
    // For updates, you typically create a new version.
    
    // If rule hasn't been compiled yet:
    rule.Expression = newExpression;
    
    await context.SaveChangesAsync();
}
```

## Loading Rules

### Loading a Complete Workflow

```csharp
public async Task<Workflow?> LoadWorkflowAsync(Guid workflowId)
{
    using var context = new RulesDbContext(_options);
    
    var workflow = await context.Workflows
        .Include(w => w.Rules)
        .ThenInclude(r => r.ChildRules)
        .Include(w => w.Rules)
        .ThenInclude(r => r.DependsOnRule)
        .FirstOrDefaultAsync(w => w.Id == workflowId && w.IsActive);
    
    if (workflow == null) return null;
    
    // Filter to active rules only
    workflow.Rules = workflow.Rules
        .Where(r => r.IsActive)
        .ToList();
    
    return workflow;
}
```

### Loading Rules into a Batch

```csharp
public async Task<RuleBatch> LoadBatchForTenantAsync(string tenantId)
{
    using var context = new RulesDbContext(_options);
    
    var rules = await context.Rules
        .Where(r => r.Workflow.Description == tenantId && r.IsActive)
        .OrderByDescending(r => r.Priority)
        .ToListAsync();
    
    var batch = new RuleBatch();
    batch.AddRules(rules);
    
    return batch;
}
```

### Hot-Reloading Rules

```csharp
public class RuleService
{
    private readonly RulesDbContext _context;
    private Workflow? _cachedWorkflow;
    private DateTime _lastLoaded = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Workflow> GetWorkflowAsync(Guid workflowId)
    {
        if (_cachedWorkflow != null && 
            _cachedWorkflow.Id == workflowId &&
            DateTime.Now - _lastLoaded < _cacheDuration)
        {
            return _cachedWorkflow;
        }

        var workflow = await _context.Workflows
            .Include(w => w.Rules)
            .FirstOrDefaultAsync(w => w.Id == workflowId && w.IsActive);

        if (workflow == null)
            throw new KeyNotFoundException($"Workflow {workflowId} not found");

        _cachedWorkflow = workflow;
        _lastLoaded = DateTime.Now;

        return workflow;
    }

    public void InvalidateCache()
    {
        _cachedWorkflow = null;
    }
}
```

## JSON Configuration Column

Store complex rule configurations as JSON for flexibility.

### JSON Column Approach

```csharp
public class RuleConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string RuleJson { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

// Store
var config = new RuleConfiguration
{
    Name = "Customer validation",
    RuleJson = JsonConvert.SerializeObject(new
    {
        Description = "Adult check",
        Expression = "customer.Age >= 18",
        Action = "customer.IsAdult = true",
        Priority = 10
    })
};

// Load
var loaded = JsonConvert.DeserializeObject<Rule>(config.RuleJson);
```

### Hybrid Approach (Recommended)

Store simple properties in columns, complex nested structures in JSON:

```csharp
modelBuilder.Entity<Rule>(entity =>
{
    // Standard columns for querying
    entity.Property(e => e.Description).HasMaxLength(500);
    entity.Property(e => e.IsActive);
    entity.Property(e => e.Priority);
    
    // JSON column for child rules (SQL Server 2016+)
    entity.Property(e => e.ChildRulesJson)
        .HasColumnType("NVARCHAR(MAX)");
});
```

## Rule Versioning with Temporal Tables

SQL Server temporal tables track all changes automatically.

### Enabling Temporal Tables

```csharp
modelBuilder.Entity<Workflow>(entity =>
{
    entity.ToTable("Workflows", b => b.IsTemporal());
});

modelBuilder.Entity<Rule>(entity =>
{
    entity.ToTable("Rules", b => b.IsTemporal());
});
```

### Querying Historical Data

```csharp
// Get current version
var current = await context.Rules.FindAsync(ruleId);

// Get version as of a specific date
var historical = await context.Rules
    .TemporalAsOf(new DateTime(2026, 1, 15))
    .FirstOrDefaultAsync(r => r.Id == ruleId);

// Get all versions between dates
var allVersions = await context.Rules
    .TemporalBetween(
        new DateTime(2026, 1, 1),
        new DateTime(2026, 3, 1))
    .Where(r => r.Id == ruleId)
    .OrderBy(r => EF.Property<DateTime>(r, "PeriodStart"))
    .ToListAsync();
```

### Audit Query

```csharp
public async Task<List<RuleChange>> GetRuleHistoryAsync(Guid ruleId)
{
    var history = await context.Rules
        .TemporalAll()
        .Where(r => r.Id == ruleId)
        .Select(r => new RuleChange
        {
            RuleId = r.Id,
            Expression = r.Expression,
            Action = r.Action,
            IsActive = r.IsActive,
            PeriodStart = EF.Property<DateTime>(r, "PeriodStart"),
            PeriodEnd = EF.Property<DateTime>(r, "PeriodEnd")
        })
        .ToListAsync();

    return history;
}

public class RuleChange
{
    public Guid RuleId { get; set; }
    public string Expression { get; set; } = "";
    public string Action { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
```

## Querying Rules

### Find Rules by Description

```csharp
var rules = await context.Rules
    .Where(r => r.Description.Contains("fraud") && r.IsActive)
    .OrderByDescending(r => r.Priority)
    .ToListAsync();
```

### Find Workflows with No Active Rules

```csharp
var emptyWorkflows = await context.Workflows
    .Where(w => !w.Rules.Any(r => r.IsActive))
    .ToListAsync();
```

### Dependency Analysis

```csharp
// Find all rules that depend on a specific rule
var dependents = await context.Rules
    .Where(r => r.DependsOnRuleId == ruleId)
    .ToListAsync();

// Find all rules with dependencies
var withDeps = await context.Rules
    .Where(r => r.DependsOnRuleId != null)
    .Include(r => r.DependsOnRule)
    .ToListAsync();
```

## Performance Considerations

### Compilation Caching

Rules should be compiled once and reused:

```csharp
public class CompiledRuleService
{
    private readonly ConcurrentDictionary<Guid, CompiledWorkflow> _cache = new();

    public async Task<CompiledWorkflow> GetCompiledAsync(Guid workflowId)
    {
        if (_cache.TryGetValue(workflowId, out var compiled))
            return compiled;

        var workflow = await LoadWorkflowAsync(workflowId);
        workflow.Validate();
        workflow.Compile(GetParameters());

        var entry = new CompiledWorkflow(workflow);
        _cache[workflowId] = entry;
        return entry;
    }
}

public class CompiledWorkflow
{
    public Workflow Workflow { get; }
    public DateTime CompiledAt { get; }

    public CompiledWorkflow(Workflow workflow)
    {
        Workflow = workflow;
        CompiledAt = DateTime.UtcNow;
    }
}
```

### Lazy Loading vs Eager Loading

```csharp
// Eager loading — load all at once (recommended for rules)
var workflow = await context.Workflows
    .Include(w => w.Rules)
    .ThenInclude(r => r.ChildRules)
    .FirstAsync(w => w.Id == id);

// Lazy loading — load on demand (can cause N+1)
var workflow = await context.Workflows.FindAsync(id);
foreach (var rule in workflow.Rules) // DB query per iteration
{
    Console.WriteLine(rule.Description);
}
```

### Indexing Recommendations

```sql
-- For rule lookups by workflow
CREATE INDEX IX_Rules_WorkflowId ON Rules(WorkflowId) WHERE IsActive = 1;

-- For dependency lookups
CREATE INDEX IX_Rules_DependsOn ON Rules(DependsOnRuleId) WHERE DependsOnRuleId IS NOT NULL;

-- For priority ordering
CREATE INDEX IX_Rules_Priority ON Rules(WorkflowId, Priority DESC) WHERE IsActive = 1;

-- For temporal queries (automatic with temporal tables)
-- PeriodStart and PeriodEnd are indexed automatically
```

## See Also

- [Real-World Use Cases](real-world-use-cases.md)
- [Rule Action Chaining](rule-action-chaining.md)
- [API Reference: Workflow](../api-reference.md#workflow)
