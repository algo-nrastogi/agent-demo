# How the Ticket Dev Agent works

## Architecture of the agent itself

```
 ┌───────────────┐   "Implement PROJ-1234:                     ┌───────────────────────┐
 │   Developer    │    <pasted description + AC>"               │                        │
 │  (VS Code +    │ ───────────────────────────────────────────▶│  Ticket Dev Agent       │
 │  Copilot Chat) │                                              │  (.github/agents/       │
 │                │◀─────────────────────────────────────────── │   ticket-dev-agent      │
 └───────────────┘  diff, test results, git commands to run      │   .agent.md)            │
                                                                  └───────────┬────────────┘
                                                                              │ tool calls
                                              ┌────────────────────────────────┼────────────────────────────────┐
                                              ▼                                  ▼                                  ▼
                                   ┌─────────────────────┐          ┌──────────────────────┐          ┌──────────────────┐
                                   │ VS Code built-in       │          │ Local terminal          │          │ Repo instructions  │
                                   │ tools                  │          │ (dotnet test, etc.       │          │ (always injected)   │
                                   │ codebase/search/        │          │ via runCommands)          │          │ .github/copilot-    │
                                   │ editFiles/createFile     │          └──────────────────────┘          │ instructions.md +    │
                                   └─────────────────────┘                                                  │ *.instructions.md    │
                                                                                                              └──────────────────┘
```

There is **no external tool integration** in this setup — no Jira, no Bitbucket, no MCP server.
Everything the agent needs comes from the pasted ticket text plus your local workspace.

## Why this is layered the way it is

| Layer | File(s) | Scope | Why separate from the others |
|---|---|---|---|
| **Always-on repo instructions** | `.github/copilot-instructions.md` | Every Copilot Chat request, every agent, every mode | The stable "house style" — tech stack, folder layout, naming, Result pattern. Rarely changes. |
| **Path-specific instructions** | `.github/instructions/*.instructions.md` | Auto-applied only when Copilot touches a matching file glob | Keeps the always-on file short; detailed handler/test rules only load when relevant, so they don't dilute unrelated requests (e.g. editing a `.csproj`). |
| **The agent persona** | `.github/agents/ticket-dev-agent.agent.md` | Only when the developer explicitly selects this agent | Defines the *workflow* (understand → plan → code → tests → handoff) and the *tool allow-list*. This is what makes it "an agent" rather than just instructions — it has a defined sequence of phases and a restricted, entirely local toolset. |
| **Prompt files** | `.github/prompts/*.prompt.md` | Invoked on demand via `/name` | One-shot task templates for the two most common invocations (full ticket implementation, test backfill), including a placeholder for pasting the ticket text, so a teammate doesn't have to remember the exact phrasing. |

## Phase-by-phase behavior (matches the agent file)

1. **Understand** — read the pasted ticket text, restate acceptance criteria, flag gaps. If only
   a bare ticket key was given with no description, stop and ask for the text — never invent it.
2. **Locate** — find the right module/pattern via `codebase`/`search`, propose a file plan.
3. **Implement** — generate Command/Query/Handler/Validator/API code per the instructions files.
4. **Test** — generate xUnit + FakeItEasy tests, run them (`runCommands`/`runTasks`), iterate to green.
5. **Wrap-up** — summarize the diff, then hand back the exact `git checkout -b` / `git commit` /
   `git push` commands as text. The agent never runs `git push`, never touches Bitbucket, and
   never opens a PR — that's entirely on the developer.

## Guardrails baked into the agent, not left to chance
- Tool allow-list in the frontmatter is entirely local (codebase search, file edits, running
  commands) — there is no tool present that could push to a remote or call an external API, so
  the agent physically cannot skip the human handoff step even if asked to.
- "Never invent ticket content" is stated explicitly, because an LLM asked to "implement a
  ticket" from just a key will otherwise happily hallucinate acceptance criteria that sound
  plausible.
- The agent is told to *stop and ask* (or state an assumption in writing) rather than silently
  guess at ambiguous business rules — this is the single highest-leverage guardrail for review
  quality.
