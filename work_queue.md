# Today's Work Queue

Date: 2026-05-31

## Decisions / Notes

- **Child rule execution remains bottom-up.** Conditional evaluation (only run children if parent passes) is noted as a potential future addition but NOT implemented today.
- **RuleBatch is fully implemented and tested.** Removed from queue.
- **Rule result caching** — Noted for future, do NOT implement today.
- **Expression compilation caching** — Noted for future, do NOT implement today.
- **Benchmarks** — Noted for future, do NOT implement today.

## Rules Engine Features (Remaining)

1. ~~Rule result streaming~~ ✅ Implemented
2. ~~Hot-reload rules~~ — Deferred to future
6. ~~Rule versioning~~ — **Excluded** — SQL Server temporal tables + EF Core handle this natively when rules are stored in DB
7. ~~Rule action chaining~~ ✅ Implemented — DependsOnRuleId, topological sort, cycle detection, RuleContext
10. **Rule dependency graph** — Visualize rule relationships

## Documentation & Examples

13. **Examples, use cases & docs** — Real-world examples (form validation, transaction screening, feature flags, compliance checks), EF serialization guide, when to use Workflow vs RuleBatch vs individual Rule

## Deferred / Excluded

- ~~Rule serialization to EF~~ — Consolidated into #13
- ~~Distributed evaluation~~ — **Permanently excluded**
- ~~Rule templates~~ — Deferred to future
- ~~Conditional child rules~~ — Deferred to future (child execution stays bottom-up)
- ~~Rule metrics~~ — Deferred to future
- ~~Pre-compiled rule library~~ — Deferred to future

## Status

- [ ] Not started
