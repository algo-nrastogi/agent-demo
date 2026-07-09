using FluentAssertions;
using Orders.Application.Commands.CreateOrder;
using Xunit;

namespace Orders.Application.Tests.Commands;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new[] { new CreateOrderLineDto("SKU-001", 1, 9.99m) });

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_HasError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.Empty,
            new[] { new CreateOrderLineDto("SKU-001", 1, 9.99m) });

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Customer id is required.");
    }

    [Theory]
    [InlineData("", 1, 9.99, "SKU is required.")]
    [InlineData("SKU-001", 0, 9.99, "Quantity must be greater than zero.")]
    [InlineData("SKU-001", -1, 9.99, "Quantity must be greater than zero.")]
    [InlineData("SKU-001", 1, -0.01, "Unit price cannot be negative.")]
    public void Validate_WithInvalidLine_HasExpectedError(
        string sku, int quantity, decimal unitPrice, string expectedMessage)
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new[] { new CreateOrderLineDto(sku, quantity, unitPrice) });

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_WithNoLines_HasError()
    {
        // Arrange
        var command = new CreateOrderCommand(Guid.NewGuid(), Array.Empty<CreateOrderLineDto>());

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "At least one order line is required.");
    }
}
