# AOT Compatibility

RoslynRules can be referenced from AOT-published applications, but with important limitations.

## AOT-Safe APIs

The following features work correctly in AOT context:

| Feature | AOT Safe | Notes |
|---------|----------|-------|
| Workflow/Rule model creation | Yes | Plain object initialization |
| `Workflow.Validate()` | Yes | Syntax validation only, no compilation |
| Dependency graph building | Yes | `GraphAlgorithms.TopologicalSort` |
| `RuleResult` manipulation | Yes | Immutable value type |
| `RuleContext` | Yes | Thread-safe result storage |
| `Rule.Compile()` | **No** | Requires Roslyn runtime compilation |
| `Rule.Execute()` | **No** | Requires compiled delegate |
| `ExpressionCompiler` | **No** | Dynamically generates assemblies |

## Why Compilation Requires JIT

RoslynRules uses Roslyn at runtime to compile rule expressions into executable delegates:

```csharp
// This requires JIT compilation - not AOT-safe
rule.Compile(compiler, parameters);
var result = rule.Execute(parameters);
```

The `[RequiresUnreferencedCode]` attribute on these APIs produces trim analysis warnings when used in AOT context. This is expected and correct.

## Usage Pattern for AOT Apps

If your application needs AOT but also needs rule execution, consider these options:

1. **Pre-compile rules at build time** — compile rules during development and serialize the compiled delegates
2. **Host rules engine in a separate JIT process** — use a microservice or separate process for rule execution
3. **Use AOT-safe validation only** — validate rule structure in the AOT app, compile in a companion service

## AOT Tests

AOT compatibility tests live in the main test project under `RoslynRules.Tests/AotCompatibility/`.
These validate that AOT-safe APIs compile without linker errors:

```bash
dotnet test RoslynRules.Tests --filter "FullyQualifiedName~AotCompatibilityTests"
```

This runs 8 tests covering model creation, validation, `RuleResult`, `RuleContext`, and execution order.

## CI Integration

The `.github/workflows/aot.yml` workflow runs on every PR affecting `RoslynRules/` or `RoslynRules.Tests/`:

- Builds the test project with AOT analysis enabled
- Runs the `AotCompatibilityTests` xUnit test suite
- Publishes a native AOT binary on Linux (smoke test to validate trim annotations)

## Known Limitations

- `System.Linq.Expressions` is not AOT-friendly; RoslynRules uses it indirectly
- `Assembly.LoadFrom` and `Reflection.Emit` are unsupported in native AOT
- Dynamic generic instantiation (`MakeGenericType`) may be trimmed

All JIT-dependent APIs are annotated with `[RequiresUnreferencedCode]` to surface these limitations at build time.
