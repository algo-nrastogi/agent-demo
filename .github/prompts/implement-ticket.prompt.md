---
mode: agent
description: "Implement a ticket (pasted description + acceptance criteria) end-to-end in this Modular Monolith, with tests."
---

Implement the following ticket in this repository.

**Ticket key** (optional, for branch/commit naming only): `${input:ticketKey:e.g. PROJ-1234, or leave blank}`

**Title / description / acceptance criteria** (paste below):
${input:ticketText:Paste the full ticket description and acceptance criteria here}

Follow the Ticket Dev Agent workflow:
1. Restate the requirement (summary + acceptance criteria) before writing any code. Flag any
   gaps or ambiguity instead of guessing.
2. Identify the target module and propose a short file plan.
3. Implement the Command/Query + Handler + Validator + API endpoint per
   `.github/instructions/cqrs-handlers.instructions.md`.
4. Write xUnit + FakeItEasy tests per `.github/instructions/unit-tests.instructions.md` and run
   them until green.
5. Summarize the diff and give me the exact `git` commands to branch/commit — do not push or
   open a PR yourself.
