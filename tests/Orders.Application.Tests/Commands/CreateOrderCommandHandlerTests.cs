using FakeItEasy;
using FluentAssertions;
using Orders.Application.Commands.CreateOrder;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Entities;
using Xunit;

namespace Orders.Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = A.Fake<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = A.Fake<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = A.Fake<IDateTimeProvider>();
    private readonly CreateOrderCommandHandler _sut;

    public CreateOrderCommandHandlerTests()
    {
        A.CallTo(() => _dateTimeProvider.UtcNow).Returns(new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc));
        _sut = new CreateOrderCommandHandler(_orderRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessAndPersistsOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Lines: new[] { new CreateOrderLineDto("SKU-001", 2, 19.99m) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        A.CallTo(() => _orderRepository.AddAsync(
                A<Order>.That.Matches(o => o.CustomerId == command.CustomerId && o.Lines.Count == 1),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithNoLines_ReturnsValidationFailureAndDoesNotPersist()
    {
        // Arrange
        // Note: ValidationBehavior would normally reject this before the handler runs;
        // this test exercises the handler's own defense-in-depth check on the domain invariant.
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Lines: Array.Empty<CreateOrderLineDto>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation");

        A.CallTo(() => _orderRepository.AddAsync(A<Order>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WithZeroQuantityLine_ReturnsValidationFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Lines: new[] { new CreateOrderLineDto("SKU-001", 0, 19.99m) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation");
        A.CallTo(() => _orderRepository.AddAsync(A<Order>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepositoryAndUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Lines: new[] { new CreateOrderLineDto("SKU-001", 1, 9.99m) });

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        A.CallTo(() => _orderRepository.AddAsync(A<Order>._, cts.Token))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(cts.Token))
            .MustHaveHappenedOnceExactly();
    }
}
