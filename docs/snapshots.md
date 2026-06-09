---
layout: default
title: Snapshots
parent: Documentation
nav_order: 8
---

# Snapshots

Snapshots provide **AOT-safe rule persistence** by capturing compiled rule state. JIT environments create snapshots; AOT environments consume them.

---

## What Are Snapshots?

A snapshot is an immutable, serializable representation of a compiled workflow or rule. It captures:

- Rule metadata (ID, description, version, expression strings)
- Compilation results (compiled delegates)
- Dependency chains (child rules, `DependsOnRuleId`)

Snapshots **do not** include runtime parameter values or execution results.

---

## JIT vs AOT Roles

| Capability | JIT | AOT |
|-----------|-----|-----|
| Create snapshots from compiled rules | Yes | No |
| Save snapshots to JSON/XML | Yes | No |
| Load snapshots from JSON/XML | Yes | Yes |
| Restore workflows from snapshots | Yes | Yes |
| Execute restored workflows | Yes | Yes |

**JIT** is the authoring/compilation environment.
**AOT** is the execution-only environment.

---

## Creating Snapshots (JIT Only)

### From a Compiled Workflow

```csharp
using RoslynRules.Snapshots;
using RoslynRules.Json;

// 1. Compile in JIT
var workflow = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
workflow.Compile(parameters);

// 2. Create snapshot
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = compiled.ToSnapshot();

// 3. Serialize to JSON
var serializer = new JsonSnapshotSerializer();
var json = serializer.Serialize(snapshot);
File.WriteAllText("workflow.snap.json", json);
```

### Using SnapshotManager

```csharp
// Compile + snapshot in one call
var snapshot = SnapshotManager.CompileAndSnapshot(
    workflow,
    parameters,
    additionalNamespaces: new[] { "MyApp.Models" }
);

// Save to file
SnapshotManager.SaveSnapshot(snapshot, serializer, "workflow.snap.json");
```

---

## Loading Snapshots (AOT Safe)

### From JSON

```csharp
// AOT-safe: no compilation, no reflection
var serializer = new JsonSnapshotSerializer();
var snapshot = SnapshotManager.LoadSnapshot(serializer, "workflow.snap.json");

// Restore workflow (rules are NOT compiled)
var workflow = SnapshotManager.RestoreWorkflow(snapshot);
```

### From XML

```csharp
var serializer = new XmlSnapshotSerializer();
var snapshot = SnapshotManager.LoadSnapshot(serializer, "workflow.snap.xml");
var workflow = SnapshotManager.RestoreWorkflow(snapshot);
```

---

## Two-Stage Deployment

```
[JIT Tool]         Compile()       [Snapshot Files]
   |            ---------------->        |
   |           ToSnapshot()             | LoadSnapshot()
   |                                    v
   |                              [AOT App]
   |                              Execute()
```

**JIT Authoring Tool:**
- Loads rules from JSON/XML/EF
- Compiles expressions with ExpressionCompiler
- Saves snapshots
- Can modify and recompile

**AOT Production App:**
- Loads snapshots at startup
- Executes pre-compiled delegates
- No runtime compilation

---

## Snapshot Formats

| Format | Serializer | AOT Safe |
|--------|-----------|----------|
| JSON | JsonSnapshotSerializer | Yes |
| XML | XmlSnapshotSerializer | Yes |

Both use source-generated serializers (System.Text.Json source generators, System.Xml.Linq) with no reflection.

---

## Restoring vs Compiling

Restored workflows from snapshots are **not compiled**:

```csharp
var restored = SnapshotManager.RestoreWorkflow(snapshot);

// Throws: rules are not compiled
// restored.Execute(parameters);

// JIT only: compile after restoring
var compiled = SnapshotManager.RestoreAndCompile(restored, parameters);
var results = compiled.Execute(parameters);
```

In AOT, you **cannot** compile.

---

## Examples

### Example 1: Full JIT Compile → Snapshot → Save → Load → Execute

```csharp
using RoslynRules.Models;
using RoslynRules.Snapshots;
using RoslynRules.Json;

// Step 1: Create a workflow with rules
var workflow = new Workflow
{
    Description = "Order validation",
    Rules =
    {
        new Rule
        {
            Description = "Minimum order amount",
            Expression = "order.Total >= 10.00",
            IsActive = true
        },
        new Rule
        {
            Description = "Customer exists",
            Expression = "customer != null",
            IsActive = true
        }
    }
};

// Step 2: Compile (JIT only)
var parameters = new[]
{
    new RuleParameter("order", typeof(Order)),
    new RuleParameter("customer", typeof(Customer))
};
workflow.Compile(parameters);

// Step 3: Create snapshot
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = compiled.ToSnapshot();

// Step 4: Save to JSON
var serializer = new JsonSnapshotSerializer();
SnapshotManager.SaveSnapshot(snapshot, serializer, "order-rules.snap.json");

// Step 5: Later... load snapshot (AOT safe)
var loadedSnapshot = SnapshotManager.LoadSnapshot(serializer, "order-rules.snap.json");
var restoredWorkflow = SnapshotManager.RestoreWorkflow(loadedSnapshot);

// Step 6: Execute with data
var order = new Order { Total = 25.00m };
var customer = new Customer { Name = "Alice" };
var results = restoredWorkflow.Execute(
    new RuleParameter("order", typeof(Order), order),
    new RuleParameter("customer", typeof(Customer), customer)
);

foreach (var result in results)
{
    Console.WriteLine($"{result.RuleDescription}: {result.IsSuccess}");
}
```

### Example 2: Child Rules and Dependencies

```csharp
var parentRule = new Rule
{
    Description = "Parent check",
    Expression = "account.Balance > 0"
};

var childRule = new Rule
{
    Description = "Overdraft protection",
    Expression = "account.Balance >= -100",
    DependsOnRuleId = parentRule.Id
};

parentRule.ChildRules.Add(childRule);

var workflow = new Workflow
{
    Description = "Account validation",
    Rules = { parentRule }
};

// Compile and snapshot
var parameters = new[] { new RuleParameter("account", typeof(Account)) };
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = compiled.ToSnapshot();

// Serialize to XML
var xmlSerializer = new XmlSnapshotSerializer();
var xml = xmlSerializer.Serialize(snapshot);
File.WriteAllText("account-rules.snap.xml", xml);
```

### Example 3: XML Round-Trip

```csharp
using RoslynRules.Xml;

// Create and compile
var workflow = new Workflow
{
    Description = "XML test",
    Rules = { new Rule { Description = "R1", Expression = "x > 0" } }
};
var param = new RuleParameter("x", typeof(int), 1);
var compiled = CompiledWorkflow.Compile(workflow, new[] { param });

// Snapshot → XML → file
var snapshot = SnapshotManager.CreateSnapshot(compiled);
var serializer = new XmlSnapshotSerializer();
SnapshotManager.SaveSnapshot(snapshot, serializer, "test.snap.xml");

// File → XML → snapshot → workflow
var loaded = SnapshotManager.LoadSnapshot(serializer, "test.snap.xml");
var restored = SnapshotManager.RestoreWorkflow(loaded);

Console.WriteLine($"Restored: {restored.Description}");
Console.WriteLine($"Rules: {restored.Rules.Count}");
```

### Example 4: Version Checking Before Execution

```csharp
var expectedVersion = new RuleVersion(2, 0, 0);

var snapshot = SnapshotManager.LoadSnapshot(serializer, "rules.snap.json");
var workflow = SnapshotManager.RestoreWorkflow(snapshot);

if (!workflow.IsVersionCompatibleWith(expectedVersion))
{
    Console.WriteLine($"Version mismatch: expected {expectedVersion}, got {workflow.Version}");
    return;
}

var results = workflow.Execute(parameters);
```

---

## Version Compatibility

Snapshots include rule versions. Loading a snapshot does not validate version compatibility:

```csharp
var snapshot = SnapshotManager.LoadSnapshot(serializer, "workflow.snap.json");
var workflow = SnapshotManager.RestoreWorkflow(snapshot);

if (!workflow.IsVersionCompatibleWith(expectedVersion))
{
    throw new InvalidOperationException("Snapshot version mismatch");
}
```
