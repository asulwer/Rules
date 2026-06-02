---
layout: default
title: API Reference
nav_order: 3
has_children: true
---

# API Reference

Complete reference for all RoslynRules public APIs, organized by component.

## Quick Navigation

### Core Models
| Class | Purpose |
|-------|---------|
| [Rule](api/rule.md) | Individual rule with Expression, Action, child rules |
| [Workflow](api/workflow.md) | Container for top-level rules |
| [RuleResult](api/ruleresult.md) | Execution result with child traceability |
| [RuleParameter](api/ruleparameter.md) | Parameter definition (name, type, value) |

### Execution & Context
| Class | Purpose |
|-------|---------|
| [RuleContext](api/rulecontext.md) | Access dependency rule results during execution |
| [IRuleEngine](api/iruleengine.md) | Abstraction for DI and mocking |
| [RuleBatch](api/rulebatch.md) | Batch evaluation for 10+ rules |

### Compilation
| Class | Purpose |
|-------|---------|
| [ExpressionCompiler](api/expressioncompiler.md) | Compile C# expressions to typed delegates |
| [Delegate Types](api/delegate-types.md) | Supported expression signatures |

### Configuration & Data
| Class | Purpose |
|-------|---------|
| [JSON Serialization](api/json-serialization.md) | Save/load rules from JSON |
| [Rule Templates](api/rule-templates.md) | Reusable parameterized rule templates |
| [Rule Predicates](api/rule-predicates.md) | Built-in validation factory methods |

### Runtime Features
| Topic | Purpose |
|-------|---------|
| [Rule Priority](api/rule-priority.md) | Control execution order |
| [Lifecycle Events](api/lifecycle-events.md) | Pre/post execution hooks |
| [Result Caching](api/result-caching.md) | Memoization for expensive rules |

### Exceptions & Diagnostics
| Class | Purpose |
|-------|---------|
| [Exceptions](api/exceptions.md) | Typed exception hierarchy |
| [ValidationError](api/exceptions.md#validationerror) | Structured validation errors |

---

**Looking for examples?** See the [Examples](examples/) section for real-world use cases.
