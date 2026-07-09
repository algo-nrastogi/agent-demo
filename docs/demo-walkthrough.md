# Step-by-Step Demo — Ticket Dev Agent

This walkthrough takes one sample ticket from "pasted into chat" to a ready-to-push local branch,
using the files in this kit. No Jira, no Bitbucket integration — everything happens in your local
workspace. Total time for a small ticket like this: ~10–15 minutes, most of it review time.

---

## 0. Prerequisites

- VS Code with the **GitHub Copilot** and **GitHub Copilot Chat** extensions.
- The solution repo cloned locally, with `.github/` and `docs/` from this kit copied into the
  repo root.
- .NET 8 SDK installed locally (`dotnet --version` → `8.x`). Run `dotnet restore TicketDevAgentKit.sln`
  once after copying this kit in, so the reference sample and its tests build.
- A Bitbucket Cloud repo the developer has push access to (used only in the final manual step —
  Copilot never touches it directly).

---

## 1. One-time setup

### 1.1 Enable custom agents & instruction files in VS Code
`Ctrl+,` → search `chat.useAgentsMdFile` and `Code Generation: Use Instruction Files` → both
enabled (on by default in recent VS Code).

### 1.2 Select the agent
In the Copilot Chat panel, open the agent dropdown (bottom of the chat input) → **Configure
Agents…** if it's not listed yet → confirm **Ticket Dev Agent** appears (VS Code auto-discovers
`.github/agents/*.agent.md`) → select it.

That's the entire setup — there's no MCP server, no OAuth flow, and no credentials to configure,
since the agent has no external integrations.

---

## 2. The sample ticket

Assume your ticket tracker (Jira or otherwise) has a ticket like this. You don't connect
anything — just copy this text into the chat when you're ready:

> **PROJ-1234 — Story: Allow customers to place an order**
> **Description:** As a customer, I want to submit an order with one or more line items so that
> the warehouse can fulfill it.
> **Acceptance criteria:**
> - An order must have a customer and at least one line item.
> - Each line item needs a SKU, a quantity > 0, and a non-negative unit price.
> - `POST /api/orders` returns 201 with the new order id on success, 400 with a clear error on
>   invalid input.

This maps 1:1 to the `Orders` module reference implementation already in `src-sample/` — that's
intentional, so you can diff what the agent produces against a known-good answer while you're
still trusting the setup.

---

## 3. Run the agent

In Copilot Chat, with **Ticket Dev Agent** selected, paste:

```
Implement PROJ-1234:

Title: Allow customers to place an order
Description: As a customer, I want to submit an order with one or more line items so that the
warehouse can fulfill it.
Acceptance criteria:
- An order must have a customer and at least one line item.
- Each line item needs a SKU, a quantity > 0, and a non-negative unit price.
- POST /api/orders returns 201 with the new order id on success, 400 with a clear error on
  invalid input.
```

(or use the slash command `/implement-ticket`, which prompts you for the ticket key and text
separately)

### What you should see, phase by phase

**Phase 1 — Understand**
> Here's what I understood from PROJ-1234:
> - Create a new Order with a customer and ≥1 line items
> - Each line: SKU (required), quantity (>0), unit price (≥0)
> - `POST /api/orders` → 201 + id on success, 400 + error message on failure
>
> No open questions — acceptance criteria are complete. Proceeding.

If your real ticket is missing details, this is where the agent should stop and ask, or state an
assumption explicitly — that's the guardrail from the agent file working as intended. If you only
type `Implement PROJ-1234` with no description at all, the agent should refuse to guess and ask
you to paste the requirement instead.

**Phase 2 — Locate**
> This is new functionality for an `Orders` module. No existing module matches closely enough to
> extend — I'll scaffold `Orders.Domain`, `Orders.Application`, `Orders.Infrastructure`,
> `Orders.Api` following the layout in `docs/architecture-guidelines.md`.
>
> Planned files:
> - `Order.cs`, `OrderLine.cs` (Domain)
> - `CreateOrderCommand.cs`, `CreateOrderCommandHandler.cs`, `CreateOrderCommandValidator.cs` (Application)
> - `IOrderRepository.cs`, `IUnitOfWork.cs` (Application/Common/Interfaces)
> - `OrderRepository.cs`, `OrdersDbContext.cs` (Infrastructure)
> - `OrdersController.cs` (Api)
> - `CreateOrderCommandHandlerTests.cs`, `CreateOrderCommandValidatorTests.cs` (tests)

**Phase 3 — Implement**
The agent creates the files. This is the point to compare its output against
`src-sample/Modules/Orders/**` in this kit — it should match the pattern closely (record commands,
primary-constructor DI, invariants inside `Order.Create`, `Result<Guid>` return type, thin
controller).

**Phase 4 — Tests**
The agent generates tests matching `tests/Orders.Application.Tests/**` in this kit, then runs:
```
dotnet test tests/Orders.Application.Tests
```
and reports something like:
```
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8
```
iterating on its own code if anything fails, before showing you the summary.

**Phase 5 — Wrap-up**
> Summary:
> - Added: 9 files (Domain, Application, Infrastructure, Api, 2 test files)
> - Tests: 8/8 passing, covering happy path, validation failures, and cancellation forwarding
> - Follow-ups for you: run `dotnet ef migrations add AddOrders` once you've reviewed the entity
>   shape; no integration tests included (out of scope for this agent)
>
> Run these yourself when you're ready:
> ```
> git checkout -b feature/PROJ-1234-place-order
> git add .
> git commit -m "PROJ-1234: Add CreateOrder command, handler, validator, and tests"
> git push -u origin feature/PROJ-1234-place-order
> ```
> Then open the pull request in Bitbucket's web UI, linking back to PROJ-1234 in the description.

---

## 4. Review the diff

Before running those `git` commands, actually read the diff:
- Does `Order.Create` reject zero line items? (domain invariant)
- Is validation in the `CreateOrderCommandValidator`, not inline in the handler?
- Are all injected dependencies interfaces, not `DbContext` directly?
- Do the tests fake dependencies with `A.Fake<T>()` and assert with FluentAssertions?

This is the same checklist a human reviewer would use — the agent is a fast first draft, not a
replacement for review.

## 5. Push and open the PR yourself

Run the `git` commands the agent gave you, then open the pull request in Bitbucket's web UI (or
your Bitbucket CLI of choice). Paste in the requirement summary and test checklist the agent gave
you in Phase 5 as the PR description. The agent has no Bitbucket access, so this step is always
manual — that's by design, not a missing feature.

## 6. Iterate

If a reviewer asks for a change (e.g. "also validate max 50 line items per order"), just reply in
the same chat:
```
Add a rule: an order can have at most 50 line items
```
The agent updates `CreateOrderCommandValidator` (or `Order.Create`, if it should be a domain
invariant rather than an input-shape rule — worth asking it to justify the choice), adds a test
for the new rule, and re-runs the suite. Commit and push the update yourself the same way as
before.

---

## 7. Extending this to other ticket types

- **Bug ticket**: same flow, but Phase 1 restates the *repro steps* and *expected vs actual*
  instead of acceptance criteria, and Phase 2 starts with `search`/`usages` to locate the faulty
  handler instead of scaffolding a new module.
- **Query-only ticket** (e.g. "add a GET endpoint to list a customer's orders"): the agent follows
  the same phases but produces a Query + QueryHandler + lean DTO projection instead of a Command,
  per `docs/architecture-guidelines.md`.
- **Cross-module ticket**: the agent should flag this in Phase 2 and propose splitting it into
  one ticket/PR per module rather than reaching across module boundaries in a single change.
