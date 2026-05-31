# Today's Work Queue

Date: 2026-05-31

## Decisions / Notes

- **Child rule execution remains bottom-up.** Conditional evaluation (only run children if parent passes) is noted as a potential future addition but NOT implemented today.
- **RuleBatch is fully implemented and tested.** Removed from queue.
- **Rule result caching** — Noted for future, do NOT implement today.
- **Expression compilation caching** — Noted for future, do NOT implement today.
- **Benchmarks** — Noted for future, do NOT implement today.

## Rules Engine Features (Remaining)

1. **Rule result streaming** — IAsyncEnumerable for large rule sets (already partially done with ExecuteAsync)
2. ~~Hot-reload rules~~ — Deferred to future
3. **Rule serialization to EF** — Example showing how to store rules in database
4. ~~Distributed evaluation~~ — **Permanently excluded**
5. ~~Rule templates~~ — Deferred to future
6. **Rule versioning** — Track rule changes over time
7. **Rule action chaining** — Output of one rule feeds into next
8. ~~Conditional child rules~~ — Deferred to future (child execution stays bottom-up)
9. ~~Rule metrics~~ — Deferred to future
10. **Rule dependency graph** — Visualize rule relationships
11. ~~Pre-compiled rule library~~ — Deferred to future
12. ~~Rule testing framework~~ ✅ Implemented

## Documentation & Examples

13. **Extensive examples, use cases & documentation** — Real-world examples (form validation, transaction screening, feature flags, compliance checks), when to use Workflow vs RuleBatch vs individual Rule, and EF serialization guide

## Status

- [ ] Not started
