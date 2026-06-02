---
layout: default
title: Documentation
nav_order: 1
has_children: false
---

# RoslynRules Documentation

High-performance .NET rules engine with Roslyn compilation, typed delegates, and async support.

### [Migration Guide](migration.md) — Moving from Microsoft.RulesEngine

## What Makes It Fast

<table>
  <thead>
    <tr><th>Feature</th><th>How It Works</th></tr>
  </thead>
  <tbody>
    <tr><td><strong>Roslyn Compilation</strong></td><td>Expressions compile to IL once, execute as native code</td></tr>
    <tr><td><strong>Typed Delegates</strong></td><td>Direct <code>Func&lt;T,R&gt;</code> calls — no <code>DynamicInvoke</code> overhead</td></tr>
    <tr><td><strong>Single Parameter</strong></td><td>One input, one output — no array allocation per call</td></tr>
    <tr><td><strong>Immutable Rules</strong></td><td>Lock after compile — zero thread contention</td></tr>
    <tr><td><strong>Parallel Execution</strong></td><td><code>Parallel.For</code> for independent rule evaluation</td></tr>
  </tbody>
</table>

## When to Use Which Execution Mode

<table>
  <thead>
    <tr><th>Mode</th><th>Best For</th></tr>
  </thead>
  <tbody>
    <tr><td><code>Execute()</code></td><td>Few simple rules, low latency requirements</td></tr>
    <tr><td><code>ExecuteParallel()</code></td><td>Many CPU-intensive rules</td></tr>
    <tr><td><code>ExecuteParallelAsync()</code></td><td>Rules with async I/O (DB, HTTP)</td></tr>
    <tr><td><code>ExecuteAsync()</code></td><td>Streaming results, async iterators</td></tr>
  </tbody>
</table>

## Example

```csharp
var rule = new Rule
{
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true"
};

var wf = new Workflow { Rules = new List<Rule> { rule } };
var compileParam = new RuleParameter("customer", typeof(Customer));
var executeParam = new RuleParameter("customer", typeof(Customer), customer);

wf.Validate();
wf.Compile(new[] { compileParam });
var results = wf.Execute(new[] { executeParam });
```

## Dependency Injection

Register `IRuleEngine` in your DI container:

```csharp
using RoslynRules.Abstractions;

services.AddSingleton<IRuleEngine, Workflow>();
```

[Get Started →](getting-started.md)

## API Reference

<a href="api-reference.html">API Reference →</a> — Complete API documentation organized by component:

<table>
  <thead>
    <tr><th>Section</th><th>Contents</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api-reference.html#core-models">Core Models</a></td><td>Rule, Workflow, RuleResult, RuleParameter</td></tr>
    <tr><td><a href="api-reference.html#execution--context">Execution</a></td><td>RuleContext, IRuleEngine, RuleBatch</td></tr>
    <tr><td><a href="api-reference.html#compilation">Compilation</a></td><td>ExpressionCompiler, Delegate Types</td></tr>
    <tr><td><a href="api-reference.html#configuration--data">Configuration</a></td><td>JSON, Templates, Predicates</td></tr>
    <tr><td><a href="api-reference.html#runtime-features">Runtime Features</a></td><td>Priority, Events, Caching</td></tr>
    <tr><td><a href="api-reference.html#exceptions--diagnostics">Exceptions</a></td><td>Exception hierarchy, ValidationError</td></tr>
  </tbody>
</table>
