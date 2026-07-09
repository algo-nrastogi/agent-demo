using FakeItEasy;
using FluentAssertions;
using Orders.Application.Commands.CancelOrder;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Entities;
using Xunit;

namespace Orders.Application.Tests.Commands;

public class CancelOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = A.Fake<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = A.Fake<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = A.Fake<IDateTimeProvider>();
    private readonly CancelOrderCommandHandler _sut;

    public CancelOrderCommandHandlerTests()
    {
        A.CallTo(() => _dateTimeProvider.UtcNow).Returns(new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc));
        _sut = new CancelOrderCommandHandler(_orderRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenOrderIsCancellable_ReturnsCancelledOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, [new OrderLine("SKU-001", 1, 9.99m)], new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc));
        var command = new CancelOrderCommand(order.Id, customerId);

        A.CallTo(() => _orderRepository.GetByIdAsync(order.Id, A<CancellationToken>._)).Returns(order);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.Status.Should().Be("CANCELLED");
        result.Value.CancelledAtUtc.Should().Be(new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc));

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid());
        A.CallTo(() => _orderRepository.GetByIdAsync(command.OrderId, A<CancellationToken>._)).Returns((Order?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NotFound");

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenOrderBelongsToDifferentCustomer_ReturnsForbidden()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), [new OrderLine("SKU-001", 1, 9.99m)], new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc));
        var command = new CancelOrderCommand(order.Id, Guid.NewGuid());

        A.CallTo(() => _orderRepository.GetByIdAsync(order.Id, A<CancellationToken>._)).Returns(order);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Forbidden");

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotCancellable_ReturnsConflict()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, [new OrderLine("SKU-001", 1, 9.99m)], new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc));
        order.Cancel(new DateTime(2026, 7, 8, 1, 0, 0, DateTimeKind.Utc));
        var command = new CancelOrderCommand(order.Id, customerId);

        A.CallTo(() => _orderRepository.GetByIdAsync(order.Id, A<CancellationToken>._)).Returns(order);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Conflict");

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepositoryAndUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, [new OrderLine("SKU-001", 1, 9.99m)], new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc));
        var command = new CancelOrderCommand(order.Id, customerId);

        A.CallTo(() => _orderRepository.GetByIdAsync(order.Id, cts.Token)).Returns(order);

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        A.CallTo(() => _orderRepository.GetByIdAsync(order.Id, cts.Token)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(cts.Token)).MustHaveHappenedOnceExactly();
    }
}
