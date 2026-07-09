---
mode: agent
description: "Generate or extend xUnit+FakeItEasy tests for the handler in the current file."
---

Generate xUnit + FakeItEasy unit tests for `${file}` following
`.github/instructions/unit-tests.instructions.md`.

Requirements:
- Cover the happy path, every validator rule that can fail, and every domain-invariant/business
  rule failure the handler can surface.
- Fake every injected interface with `A.Fake<T>()`; do not use a real DbContext.
- Use FluentAssertions for all assertions.
- Verify the cancellation token is forwarded to any repository/infrastructure call.
- Place the test file at the mirrored path under `tests/Modules/<Module>/<Module>.Application.Tests/`.
- Run the resulting test project and report the pass/fail summary.
