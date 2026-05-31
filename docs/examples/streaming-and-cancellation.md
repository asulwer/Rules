---
layout: default
title: Streaming and Cancellation
parent: Examples
nav_order: 6
---

[← Back to Examples Index](index.md)

# Streaming and Cancellation

Process large rule sets efficiently with streaming execution and cancellation support.

## Table of Contents
- [ExecuteAsync (Streaming)](#executeasync-streaming)
- [ExecuteBufferedAsync (Chunked)](#executebufferedasync-chunked)
- [CancellationToken](#cancellationtoken)
- [Parallel Async with Cancellation](#parallel-async-with-cancellation)

## ExecuteAsync (Streaming)

Use `ExecuteAsync` when you have many rules and want to process results as they arrive, without waiting for all rules to complete.

### Basic Streaming

```csharp
var workflow = new Workflow
{
    Description = "Large validation set",
    Rules = GetRulesFromDatabase() // 100+ rules
};

workflow.Compile(parameters);

// Process results as they arrive
await foreach (var result in workflow.ExecuteAsync(parameters))
{
    if (!result.Success)
    {
        LogFailure(result);
        // Can break early — remaining rules are still evaluated
        // but you stop consuming
    }
}
```

### Short-Circuit on First Failure

```csharp
await foreach (var result in workflow.ExecuteAsync(parameters))
{
    if (!result.Success)
    {
        // Stop processing and return early
        return new ValidationFailure(result);
    }
}

// All rules passed
return ValidationSuccess();
```

### Real-Time Progress Reporting

```csharp
var processed = 0;
var total = workflow.Rules.Count;

await foreach (var result in workflow.ExecuteAsync(parameters))
{
    processed++;
    
    ReportProgress(new ProgressInfo
    {
        Processed = processed,
        Total = total,
        CurrentRule = result.RuleDescription,
        Success = result.Success
    });
}
```

## ExecuteBufferedAsync (Chunked)

Process rules in fixed-size chunks for batch operations.

### Batch Processing

```csharp
var workflow = new Workflow
{
    Rules = GetRulesFromDatabase() // 1000+ rules
};

workflow.Compile(parameters);

// Process 50 rules at a time
await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 50))
{
    // chunk is RuleResult[]
    await SaveResultsToDatabase(chunk);
    
    // Or process in batches
    var failures = chunk.Where(r => !r.Success).ToList();
    if (failures.Any())
    {
        await NotifyFailures(failures);
    }
}
```

### Memory-Efficient Processing

```csharp
// With 10,000 rules, loading all results into memory at once is expensive
// Buffered execution keeps memory usage constant

await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 100))
{
    // Only 100 RuleResult objects in memory at once
    foreach (var result in chunk)
    {
        ProcessResult(result);
    }
    
    // Results can be garbage collected after this iteration
}
```

### Buffered with Dependencies

Buffered execution respects rule dependencies. Rules in a buffer execute only after their dependencies (which may be in previous buffers) complete.

```csharp
var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };
// ... more rules

// If bufferSize = 1, A is in buffer 0, B is in buffer 1
// B executes after A completes
await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 1))
{
    // chunk[0] = result for that batch
}
```

## CancellationToken

Cancel rule execution to stop processing early.

### Timeout-Based Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var results = await workflow.ExecuteParallelAsync(parameters, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Rule execution timed out after 5 seconds");
}
```

### User-Initiated Cancellation

```csharp
var cts = new CancellationTokenSource();

// Start long-running rule evaluation
var task = Task.Run(async () =>
{
    await foreach (var result in workflow.ExecuteAsync(parameters, cts.Token))
    {
        UpdateUI(result);
    }
});

// User clicks "Cancel" button
buttonCancel.Click += (s, e) => cts.Cancel();
```

### Mid-Stream Cancellation

```csharp
using var cts = new CancellationTokenSource();

await foreach (var result in workflow.ExecuteAsync(parameters, cts.Token))
{
    if (result.RuleDescription == "Critical check" && !result.Success)
    {
        // Critical rule failed — cancel remaining evaluation
        cts.Cancel();
        break;
    }
}
```

## Parallel Async with Cancellation

Combine parallel execution with cancellation for maximum throughput with safety.

```csharp
// Check 50 rules against external APIs
var workflow = new Workflow
{
    Rules = new List<Rule>
    {
        new Rule { Description = "Check API 1", Expression = "await Api1.IsHealthy()" },
        new Rule { Description = "Check API 2", Expression = "await Api2.IsHealthy()" },
        // ... 48 more
    }
};

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var results = await workflow.ExecuteParallelAsync(parameters, cts.Token);

// If any API takes longer than 10 seconds, OperationCanceledException is thrown
// Otherwise, all 50 results are available
```

### Combining with RuleBatch

```csharp
var batch = new RuleBatch()
    .AddRules(GetHealthCheckRules());

batch.Compile(parameters);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var results = await batch.EvaluateParallelAsync(parameters, cts.Token);

var healthyCount = results.Count(r => r.Success);
Console.WriteLine($"{healthyCount}/{results.Length} services healthy");
```

## Performance Considerations

| Mode | Memory | Latency | Use Case |
|------|--------|---------|----------|
| `Execute()` | All results | Low | Few rules, need all results |
| `ExecuteAsync()` | One at a time | Low | Many rules, streaming |
| `ExecuteBufferedAsync()` | Fixed chunk | Low | Many rules, batch processing |
| `ExecuteParallelAsync()` | All results | Lowest | CPU-intensive, many rules |

**Rule of thumb:**
- < 10 rules: Use `Execute()`
- 10-100 rules: Use `ExecuteParallel()` or `ExecuteParallelAsync()`
- > 100 rules: Use `ExecuteBufferedAsync()` with appropriate buffer size
- UI updates: Use `ExecuteAsync()` for real-time progress
- External APIs: Use `ExecuteParallelAsync()` with cancellation timeout

## See Also

- [API Reference: Workflow](../api-reference.md#workflow)
- [Performance Tuning](../performance.md)
- [RuleBatch](rule-batch.md)
