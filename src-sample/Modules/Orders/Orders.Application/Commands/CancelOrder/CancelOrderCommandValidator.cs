using FluentValidation;

namespace Orders.Application.Commands.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.OrderId)
            .NotEmpty()
            .WithMessage("Order id is required.");

        RuleFor(c => c.CustomerId)
            .NotEmpty()
            .WithMessage("Customer id is required.");
    }
}
