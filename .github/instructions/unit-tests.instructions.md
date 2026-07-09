---
applyTo: "tests/**/*.cs"
---

# Unit test authoring rules (xUnit + FakeItEasy)

1. Test project references: `xunit`, `xunit.runner.visualstudio`, `FakeItEasy`, `FluentAssertions`.
   Never add Moq, NSubstitute, or Shouldly to a test project in this repo.
2. **Naming**: `MethodOrHandler_Scenario_ExpectedOutcome`, e.g.
   `Handle_WhenSkuDoesNotExist_ReturnsNotFoundResult`.
3. **Structure**: every test has explicit `// Arrange`, `// Act`, `// Assert` comments — no
   exceptions, even for one-line tests.
4. **Faking dependencies**:
   ```csharp
   private readonly IOrderRepository _orderRepository = A.Fake<IOrderRepository>();
   private readonly IUnitOfWork _unitOfWork = A.Fake<IUnitOfWork>();
   private readonly IDateTimeProvider _dateTimeProvider = A.Fake<IDateTimeProvider>();
   ```
   Configure behavior with `A.CallTo(() => ...).Returns(...)`. Verify interactions with
   `A.CallTo(() => ...).MustHaveHappenedOnceExactly()` (or `.MustNotHaveHappened()`), not with
   manual boolean flags.
5. **Assertions**: use FluentAssertions exclusively — `result.IsSuccess.Should().BeTrue()`,
   `result.Value.Should().Be(...)`, `action.Should().ThrowAsync<...>()`. Do not mix in raw
   `Assert.*` calls from xUnit except `Assert.Fail` for genuinely unreachable branches.
6. **Coverage expectation per handler**: at minimum —
   - happy path (valid input → success result, correct repository/unit-of-work calls made)
   - one test per validator rule that can fail
   - one test per domain-invariant / business-rule failure the handler can surface
   - cancellation token is forwarded to repository/infrastructure calls
7. Use `[Theory]` + `[InlineData]` (or a `[ClassData]` builder) instead of duplicating near-identical
   `[Fact]` methods for validator edge cases.
8. Do not use a real EF Core `DbContext`, in-memory database provider, or Testcontainers in a
   *unit* test — that belongs in the (separate) integration test suite. Unit tests fake the
   repository interface only.
9. Keep test classes one-to-one with the handler under test; put shared fixture setup in a
   `private readonly` field initializer or constructor, not a base class, unless three or more
   test classes need identical setup.
