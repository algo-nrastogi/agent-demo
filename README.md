# Ticket → Dev Agent Kit
### For a .NET 8 Modular Monolith (CQRS + MediatR) using GitHub Copilot + Bitbucket

This kit turns GitHub Copilot into a repeatable **"Ticket Dev Agent"**: you paste a ticket's
title, description, and acceptance criteria into the chat, and it implements the feature
following your Modular Monolith / CQRS conventions and writes xUnit + FakeItEasy tests.

**No Jira integration, no Bitbucket integration.** The agent works entirely on your local
workspace and hands back the exact `git` commands to branch, commit, push, and open the PR
yourself.

## What's in here

| Path | Purpose |
|---|---|
| `.github/copilot-instructions.md` | Always-on repo instructions (tech stack, structure, conventions) |
| `.github/instructions/*.instructions.md` | Path-specific rules (auto-applied by file glob) |
| `.github/agents/ticket-dev-agent.agent.md` | **The agent itself** — persona + workflow + allowed tools |
| `.github/prompts/*.prompt.md` | Reusable slash-commands (`/implement-ticket`, `/generate-unit-tests`) |
| `docs/architecture-guidelines.md` | The Modular Monolith / CQRS conventions the agent enforces |
| `docs/agent-workflow.md` | How the agent reasons, step by step |
| `docs/demo-walkthrough.md` | **Step-by-step demo** — a sample ticket, start to PR-ready branch |
| `TicketDevAgentKit.sln` | Solution file wiring together the sample module + tests below |
| `src-sample/` | Reference module (`Orders`), each project with its own `.csproj`, showing the exact pattern to replicate |
| `tests/` | Matching xUnit + FakeItEasy tests for the reference module, with its own `.csproj` |

## Build and run the reference sample

```bash
dotnet restore TicketDevAgentKit.sln
dotnet build TicketDevAgentKit.sln
dotnet test tests/Orders.Application.Tests
```
`Orders.Api` is included as a library only (no `Program.cs`) — in a real solution it gets composed
into a single Host project alongside every other module's `Api` project, per
`docs/architecture-guidelines.md`. It's in the solution so you can see how the project references
line up, not to run standalone.

## Quick start

1. Copy `.github/` and `docs/` into the root of your actual solution repo.
2. Follow `docs/demo-walkthrough.md` §1 to enable custom agents/instructions in VS Code.
3. In VS Code Copilot Chat, select **Ticket Dev Agent** from the agent dropdown.
4. Type (or use `/implement-ticket`):
   ```
   Implement PROJ-1234:
   Title: Allow customers to place an order
   Description: As a customer, I want to submit an order with one or more line items...
   Acceptance criteria:
   - An order must have a customer and at least one line item.
   - Each line item needs a SKU, a quantity > 0, and a non-negative unit price.
   - POST /api/orders returns 201 with the new order id on success, 400 on invalid input.
   ```
5. Review the diff, run the generated tests, then run the `git` commands the agent gives you and
   open the PR in Bitbucket yourself.

See `docs/demo-walkthrough.md` for the full walkthrough.
