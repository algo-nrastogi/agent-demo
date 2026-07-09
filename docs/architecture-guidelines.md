# Architecture Guidelines — Modular Monolith (.NET 8, CQRS + MediatR)

## Why Modular Monolith
Single deployable, single database (or database-per-module), but hard boundaries between
business capabilities so any module could later be peeled off into its own service without a
rewrite. The main enforcement mechanism is **project references**: a module's `Api`/`Application`
project may only reference its own `Domain`/`Infrastructure` plus `BuildingBlocks/*` — never
another module's `Domain` or `Infrastructure`.

## Layering (per module)

```
┌─────────────────────────────┐
│   <Module>.Api               │  Controllers/Endpoints → ISender.Send(command/query)
├─────────────────────────────┤
│   <Module>.Application        │  Commands, Queries, Handlers, Validators, DTOs
│   (depends on Domain only)    │  MediatR pipeline behaviors (validation, logging)
├─────────────────────────────┤
│   <Module>.Domain             │  Entities, Value Objects, Domain Events, invariants
│   (no framework references)   │
├─────────────────────────────┤
│   <Module>.Infrastructure      │  EF Core DbContext, Repository implementations,
│   (depends on Application +    │  external integrations
│    Domain, implements its       │
│    interfaces)                  │
└─────────────────────────────┘
```

Dependency direction always points **inward**: Api → Application → Domain ← Infrastructure.
Infrastructure implements interfaces that Application defines — Application never references
Infrastructure directly (classic Dependency Inversion / Clean Architecture applied per-module).

## Cross-module communication
- **Synchronous, in-process**: publish a MediatR request that another module's handler responds
  to, using a shared contract defined in a `Contracts` project — never a direct method call into
  another module's internals.
- **Asynchronous**: domain events → integration events, dispatched via an outbox table processed
  by a background worker. Use this when Module A needs to react to something that happened in
  Module B without a synchronous dependency.
- Shared kernel: only truly universal concerns (base entity, `Result<T>`, `IUnitOfWork`,
  pipeline behaviors) live in `BuildingBlocks/*`. If two modules need to share something more
  specific than that, that's a signal that either the modules should merge or the shared thing
  belongs in an explicit `Contracts` project.

## CQRS conventions
- **Commands** change state, return `Result` / `Result<Guid>` / `Result<TDto>` — never the raw
  entity.
- **Queries** read state, return DTOs shaped for the specific UI/API need (no generic "return the
  whole entity graph" queries) — feel free to query with a lean projection (e.g. Dapper or an EF
  Core `.Select()` projection) rather than loading the full aggregate when only a few fields are needed.
- **MediatR pipeline behaviors** (registered once, applied to every request) handle: validation
  (`ValidationBehavior`), logging (`LoggingBehavior`), and — optionally — transaction wrapping for
  commands (`UnitOfWorkBehavior`). Handlers stay focused on business orchestration only.

## Result pattern
```csharp
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    // Result.Success(value) / Result.Failure(error) factory methods
}
```
Use this for *expected* outcomes (validation failed, entity not found, business rule violated).
Reserve thrown exceptions for genuinely exceptional/infrastructure failures (DB unreachable,
timeout) — those propagate to global exception-handling middleware and become a 500.

## Testing pyramid
- **Unit tests** (xUnit + FakeItEasy): handlers and domain entities, dependencies faked, fast,
  run on every commit.
- **Integration tests** (separate project, out of scope for this kit): real DbContext against a
  Testcontainers SQL instance, verifying repository + EF mapping correctness.
- **Contract/API tests** (out of scope for this kit): verify the Api layer's HTTP contract.

The Jira Dev Agent in this kit is scoped to unit tests only, matching the most common CI gate for
a feature PR — integration tests are flagged as a manual follow-up when a change touches
persistence mapping.
