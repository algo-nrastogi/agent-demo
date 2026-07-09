using BuildingBlocks.Common.Application;
using MediatR;

namespace Orders.Application.Commands.CancelOrder;

public sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime PlacedAtUtc,
    DateTime? CancelledAtUtc,
    decimal Total,
    IReadOnlyCollection<OrderLineDto> Lines);

public sealed record CancelOrderCommand(Guid OrderId, Guid CustomerId) : IRequest<Result<OrderDto>>;
