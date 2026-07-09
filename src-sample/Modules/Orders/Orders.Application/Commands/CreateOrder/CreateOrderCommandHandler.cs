using BuildingBlocks.Common.Application;
using MediatR;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Entities;

namespace Orders.Application.Commands.CreateOrder;

internal sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        Order order;
        try
        {
            // Validation of shape/format already happened in ValidationBehavior before we got here.
            // Map DTOs -> domain value objects, letting the entity enforce its own invariants.
            var lines = request.Lines
                .Select(l => new OrderLine(l.Sku, l.Quantity, l.UnitPrice))
                .ToList();

            order = Order.Create(request.CustomerId, lines, dateTimeProvider.UtcNow);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Failure<Guid>(Error.Validation(ex.Message));
        }

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
