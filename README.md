# Rules

A high-performance rewrite of [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) and its maintained [fork](https://github.com/asulwer/RulesEngine). Built for speed, with a focus on zero-overhead execution, compile-time validation, and modern .NET patterns.

📖 **Documentation:** [https://asulwer.github.io/Rules/](https://asulwer.github.io/Rules/)

## Key Differences

| Feature | RulesEngine | This Rewrite |
|---------|-------------|--------------|
| Expression compiler | DynamicExpresso | **Roslyn (Microsoft.CodeAnalysis.CSharp)** |
| Delegate invocation | `DynamicInvoke` (slow) | **Typed delegates** (direct call) |
| Parameters | Multi-parameter | **Single parameter** (wrap multiples in structs) |
| Async support | None | **Async/await in expressions** |
| Validation | Runtime only | **Compile-time validation** |
| Circular reference guard | No | **Built-in tree validation** |
| Thread safety | Mutable rules | **Immutable after compilation** |
| Execution modes | Sequential | **Sequential + Parallel + Async** |

## Architecture

```
Workflow
├── Rules[] (top-level)
│   ├── Expression: "customer.Name.Contains(\"A\")"
│   ├── Action: "customer.Processed = true"
│   └── ChildRules[] (bottom-up evaluation)
│       ├── Expression: "customer.Age > 18"
│       └── Action: "customer.IsAdult = true"
```

## Quick Start

### 1. Define Your Model

```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Processed { get; set; }
}
```

### 2. Create Rules

```csharp
var rule = new Rule
{
    Description = "Check adult customers",
    Expression = "customer.Age >= 18",
    Action = "customer.Processed = true",
    IsActive = true
};

var childRule = new Rule
{
    Description = "Name contains A",
    Expression = "customer.Name.Contains(\"A\")"
};

rule.ChildRules.Add(childRule);
```

### 3. Create Workflow and Compile

```csharp
var wf = new Workflow
{
    Description = "Customer processing",
    Rules = new List<Rule> { rule }
};

var parameters = new RuleParameter[]
{
    new RuleParameter("customer", typeof(Customer), new Customer { Name = "Alice", Age = 25 })
};

// Validate before compiling (catches syntax errors, circular refs)
wf.Validate();

// Compile once, execute many times
wf.Compile(parameters);
```

### 4. Execute

```csharp
// Sequential
var results = wf.Execute(parameters);

// Parallel (CPU-intensive rules)
var results = wf.ExecuteParallel(parameters);

// Async (rules with await)
var results = await wf.ExecuteParallelAsync(parameters);
```

## Supported Delegate Signatures

Rules support exactly **one input parameter**. Return multiple values by wrapping them in a struct or class.

| Type | Signature | Example |
|------|-----------|---------|
| Expression | `Func<TParam, bool>` | `Func<Customer, bool>` |
| Expression with composite return | `Func<TParam, TReturn>` | `Func<Customer, ValidationResult>` |
| Action | `Action<TParam>` | `Action<Customer>` |
| Async Expression | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| Async Action | `Func<TParam, Task>` | `Func<Customer, Task>` |

## Async Expressions

Rules automatically detect `await` in expressions and compile to async delegates.

```csharp
var rule = new Rule
{
    Description = "Check external price",
    Expression = "await GetPriceAsync(customer.ProductId) > 100"
};

wf.Compile(parameters);
var results = await wf.ExecuteParallelAsync(parameters);
```

## Validation

Call `Validate()` before `Compile()` to catch errors early:

```csharp
wf.Validate();  // Throws on:
                // - Empty rules (no Expression, Action, or children)
                // - Invalid C# syntax in expressions
                // - Circular child references
                // - Duplicate rule IDs
                // - No active rules
```

## Compile-Time Immutability

After `Compile()`, rule properties are locked. Attempting to modify throws:

```csharp
wf.Compile(parameters);
rule.Description = "New name";  // InvalidOperationException!
```

## Logging

Rules supports `Microsoft.Extensions.Logging`. Set `rule.Logger` to any `ILogger` implementation (Serilog, NLog, etc.):

```csharp
// With Serilog
rule.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Automatic structured output:
// [15:35:12.345] [PASS] Adult check (Id: abc-123) — 0.042ms
```

**Event IDs for log filtering:**
- `1001` — RuleSkipped
- `1002` — RulePassed
- `1003` — RuleFailed
- `1004` — RuleError

## Performance

Typical execution for 999 customers:

| Mode | Time | Per Customer |
|------|------|-------------|
| Sequential | 2ms | 0.002ms |
| Parallel | 3ms | 0.003ms |
| Validation | 46ms | One-time |
| Compilation | 812ms | One-time, cached |

## Project Structure

```
Rules/
├── Models/
│   ├── Rule.cs              # Individual rule with Expression/Action/Children
│   ├── Workflow.cs          # Container with sequential/parallel/async execution
│   ├── RuleParameter.cs     # Parameter definition (name, type, value)
│   ├── RuleResult.cs        # Execution result (success, data)
│   └── CompiledDelegate.cs  # Typed delegate wrappers (no DynamicInvoke)
├── Compiler/
│   ├── ExpressionCompiler.cs   # Public API: Compile(expression) -> Delegate
│   ├── CodeGenerator.cs        # Generates C# source from expression string
│   ├── AssemblyCompiler.cs     # Compiles source to assembly bytes
│   └── DelegateFactory.cs      # Loads assembly and creates typed delegate
└── Utilities/
    └── Execute.cs           # Execution helpers
```

## Requirements

- .NET Standard 2.1 (library)
- .NET 8+ (demo project)
- NuGet: `Microsoft.CodeAnalysis.CSharp`

## License

MIT License — see [LICENSE.txt](LICENSE.txt) for details.
