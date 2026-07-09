using BuildingBlocks.Common.Application;
using MediatR;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Entities;

namespace Orders.Application.Commands.CancelOrder;

internal sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound($"Order '{request.OrderId}' was not found."));

        if (order.CustomerId != request.CustomerId)
            return Result.Failure<OrderDto>(new Error("Forbidden", "You are not allowed to cancel this order."));

        try
        {
            order.Cancel(dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict(ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString().ToUpperInvariant(),
            order.PlacedAtUtc,
            order.CancelledAtUtc,
            order.Total,
            order.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList());
    }
}
