# Copilot Instructions — [Order Management](/docs/architecture-guidelines.md#order-management)  

## What this app is
A .NET 8 **Modular Monolith**. Each business capability lives in its own module under
`src/Modules/<ModuleName>` with its own Domain/Application/Infrastructure/Api layers.
Modules communicate only through MediatR requests/notifications or well-defined contracts —
never by referencing another module's internals directly.

## Tech stack
- .NET 8, C# 12
- CQRS via **MediatR** (`IRequest<T>` / `IRequestHandler<T,TResponse>`)
- **FluentValidation** for command/query validation, wired in via a MediatR pipeline behavior
- EF Core 8 per-module `DbContext` (schema-per-module in a shared database, or separate DB per module — see `docs/architecture-guidelines.md`)
- **xUnit** + **FakeItEasy** for unit tests (never Moq, never NSubstitute — this repo standardizes on FakeItEasy)
- **FluentAssertions** for test assertions
- Serilog for structured logging
- Source control: **Bitbucket** (branch-per-ticket, PR required, no direct pushes to `main`/`develop`).
  Copilot has no Bitbucket access — it hands back plain `git` commands for the developer to run
  and push themselves.
- Work tracking: **Jira**, but Copilot has no Jira integration. The developer pastes the ticket
  title, description, and acceptance criteria directly into the chat prompt; Copilot never
  fetches or writes back to a tracker.

## Project / folder structure (per module)
```
src/Modules/<Module>/
  <Module>.Domain/           # Entities, value objects, domain events — no framework deps
  <Module>.Application/      # Commands, Queries, Handlers, Validators, DTOs, interfaces
    Commands/<Feature>/<Feature>Command.cs
    Commands/<Feature>/<Feature>CommandHandler.cs
    Commands/<Feature>/<Feature>CommandValidator.cs
    Queries/<Feature>/<Feature>Query.cs
    Queries/<Feature>/<Feature>QueryHandler.cs
    Common/Interfaces/       # Repository & infra abstractions owned by this module
    Common/Behaviors/        # Module-specific MediatR pipeline behaviors (rare; prefer BuildingBlocks)
  <Module>.Infrastructure/   # EF Core DbContext, repository implementations, external clients
  <Module>.Api/              # Minimal API endpoints or Controllers — thin, call MediatR only
tests/Modules/<Module>/
  <Module>.Application.Tests/  # Handler + validator unit tests (xUnit + FakeItEasy)
  <Module>.Domain.Tests/        # Entity/aggregate behavior tests
src/BuildingBlocks/
  Common.Domain/             # Base entity, IAggregateRoot, domain event dispatching
  Common.Application/        # ValidationBehavior, LoggingBehavior, IUnitOfWork
  Common.Infrastructure/     # Cross-cutting infra (outbox, EF conventions)
```

## Coding conventions Copilot must follow
- **One command/query = one feature folder.** Do not add new methods to existing handlers to
  cover a new use case — create a new Command/Query.
- Handlers are `internal sealed class`, registered automatically via MediatR assembly scanning
  (do not hand-register handlers in `Program.cs`).
- Commands/Queries are `public sealed record` implementing `IRequest<TResponse>`.
- All validation lives in a **FluentValidation** `AbstractValidator<TCommand>` next to the
  command, never inline in the handler. Validation is enforced via `ValidationBehavior<TRequest,TResponse>`
  in `Common.Application/Behaviors` — do not call `validator.Validate(...)` manually in a handler.
- Handlers depend only on interfaces defined in `<Module>.Application/Common/Interfaces`
  (e.g. `IOrderRepository`, `IUnitOfWork`, `IDateTimeProvider`). Never reference EF Core or
  `DbContext` directly from a handler.
- Domain invariants (e.g. "an order must have at least one line item") are enforced inside the
  **entity/aggregate**, not the handler. Handlers orchestrate; entities protect invariants.
- API layer (`<Module>.Api`) endpoints/controllers do exactly three things: map the HTTP request
  to a Command/Query, call `ISender.Send(...)`, map the result to an HTTP response. No business
  logic in controllers.
- Use `Result<T>` (see `Common.Application/Result.cs`) for expected failures (validation, not
  found, conflict). Reserve exceptions for truly exceptional/infrastructure failures.
- Async all the way down; every I/O-bound method takes a `CancellationToken` as its last parameter
  and forwards it.

## Testing conventions (also see `.github/instructions/unit-tests.instructions.md`)
- One test class per handler: `<Feature>CommandHandlerTests` / `<Feature>QueryHandlerTests`.
- Mock every injected interface with **FakeItEasy** (`A.Fake<IOrderRepository>()`), never spin
  up a real `DbContext` or hit real infrastructure in a unit test.
- Follow **Arrange / Act / Assert** with those comments as section markers.
- Cover: the happy path, each validation failure branch, and each domain-invariant failure branch.
- Assertions use **FluentAssertions** (`result.Should().BeEquivalentTo(...)`), not raw `Assert.Equal`.

## Ticket linkage (manual, no integration)
- The developer pastes the ticket key, title, description, and acceptance criteria into the
  prompt. Copilot never looks this up itself and never invents missing details.
- Branch naming: `feature/PROJ-1234-short-slug` or `bugfix/PROJ-1234-short-slug`, using whatever
  key/slug the developer provided.
- Every commit message starts with the ticket key: `PROJ-1234: <summary>`.
- Copilot proposes the branch/commit/push commands as text — it does not run `git push` or touch
  Bitbucket itself. The developer runs those commands and opens the PR manually.
- PR description (for the developer to paste in) should include: a summary of the change and a
  test checklist.

## What NOT to do
- Do not add a new NuGet package without calling it out explicitly for human approval.
- Do not reference one module's `Domain`/`Infrastructure` project from another module.
- Do not generate migrations automatically — propose the EF model change and let the developer
  run `dotnet ef migrations add` locally.
- Do not invent ticket details — if a requirement is ambiguous or fields are missing, ask or
  state the assumption explicitly instead of guessing silently.
- Do not attempt to push branches, create pull requests, or call any Jira/Bitbucket API —
  Copilot has no credentials or tool access for those systems in this setup.
