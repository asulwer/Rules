# Examples

## Basic Boolean Rule

```csharp
var rule = new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18",
    IsActive = true
};
```

## Rule with Action

```csharp
var rule = new Rule
{
    Description = "Mark adult",
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true",
    IsActive = true
};
```

## Parent with Child Rules

Children evaluated bottom-up. Parent only runs if all children pass.

```csharp
var parent = new Rule
{
    Description = "Full validation",
    Expression = "customer.IsValid"
};

var child1 = new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
};

var child2 = new Rule
{
    Description = "Age check",
    Expression = "customer.Age > 0"
};

parent.ChildRules.Add(child1);
parent.ChildRules.Add(child2);
```

## Async Rule

Auto-detected by `await` keyword. Compiled to `Func<T, Task<bool>>`.

```csharp
var rule = new Rule
{
    Description = "Check price from API",
    Expression = "await PriceService.GetAsync(customer.ProductId) > 100"
};

// Compile normally
workflow.Compile(parameters);

// Execute async
var results = await workflow.ExecuteParallelAsync(parameters);
```

## Returning Multiple Values

Wrap outputs in a struct:

```csharp
public struct ValidationResult
{
    public bool IsAdult { get; set; }
    public bool IsPremium { get; set; }
}

var rule = new Rule
{
    Expression = "new ValidationResult { 
        IsAdult = customer.Age >= 18,
        IsPremium = customer.Age >= 65
    }"
};

// Access result.Data as ValidationResult
```

## Workflow with Multiple Rules

```csharp
var workflow = new Workflow
{
    Description = "Customer processing",
    Rules = new List<Rule>
    {
        new Rule { Expression = "customer.Age >= 18", Description = "Adult" },
        new Rule { Expression = "customer.Name.StartsWith(\"A\")", Description = "Starts with A" },
        new Rule { 
            Expression = "customer.IsActive",
            Action = "customer.Processed = true",
            Description = "Process active"
        }
    }
};

workflow.Validate();
workflow.Compile(parameters);

// All rules evaluated, results in order
var results = workflow.ExecuteParallel(parameters);
```

## Logging with Serilog

```csharp
using Serilog;
using Microsoft.Extensions.Logging;

// Create a Serilog logger
var serilog = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var logger = new SerilogLoggerFactory(serilog).CreateLogger<Rule>();

// Attach to any rule
var rule = new Rule
{
    Description = "Check adult",
    Expression = "customer.Age >= 18",
    Logger = logger
};

// Execute — output appears in console:
// [15:42:10.123] [PASS] Check adult (Id: ...) — 0.031ms
```

## ExpandoObject (Dynamic)

Use when the data shape is not known at compile time. Slower but flexible.

```csharp
using System.Dynamic;

dynamic customer = new ExpandoObject();
customer.Name = "Alice";
customer.Age = 25;

var parameters = new[]
{
    new RuleParameter("customer", typeof(object), customer)
};

var rule = new Rule
{
    Description = "Check adult",
    Expression = "((dynamic)customer).Age >= 18",
    IsActive = true
};

var compiler = new ExpressionCompiler();
rule.Compile(compiler, parameters, new[] { "System.Dynamic" });

var result = rule.Execute(parameters);
// result.Success = true
```

**Caveat:** Missing properties return `null`, not throw. Test for existence if needed:
```csharp
Expression = "((dynamic)customer).Age != null && ((dynamic)customer).Age >= 18"
```

## JSON Configuration

Store rules in JSON files for configuration-driven setups:

```csharp
using Rules.Extensions;

// Load from JSON
var workflow = JsonRuleLoader.LoadFromFile("customer-rules.json");

// Validate, compile, execute as normal
workflow.Validate();
workflow.Compile(parameters);
var results = workflow.Execute(parameters);
```

**customer-rules.json:**
```json
{
  "description": "Customer validation",
  "rules": [
    {
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "isActive": true
    },
    {
      "description": "Name required",
      "expression": "!string.IsNullOrEmpty(customer.Name)",
      "isActive": true
    }
  ]
}
```

## Validation Before Compile

```csharp
try
{
    workflow.Validate();
    workflow.Compile(parameters);
}
catch (InvalidOperationException ex)
{
    // Catches:
    // - Empty rules
    // - Syntax errors in expressions
    // - Circular child references
    // - Duplicate rule IDs
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```
