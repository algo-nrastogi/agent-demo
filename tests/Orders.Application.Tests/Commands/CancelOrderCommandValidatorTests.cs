using FluentAssertions;
using Orders.Application.Commands.CancelOrder;
using Xunit;

namespace Orders.Application.Tests.Commands;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _sut = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false, "Order id is required.")]
    [InlineData(false, true, "Customer id is required.")]
    public void Validate_WhenRequiredIdsAreMissing_HasExpectedError(
        bool useEmptyOrderId,
        bool useEmptyCustomerId,
        string expectedMessage)
    {
        // Arrange
        var command = new CancelOrderCommand(
            useEmptyOrderId ? Guid.Empty : Guid.NewGuid(),
            useEmptyCustomerId ? Guid.Empty : Guid.NewGuid());

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_WhenBothIdsAreMissing_HasBothErrors()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(["Order id is required.", "Customer id is required."]);
    }
}
