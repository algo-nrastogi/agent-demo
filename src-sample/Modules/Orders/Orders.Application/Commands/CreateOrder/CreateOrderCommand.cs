using BuildingBlocks.Common.Application;
using MediatR;

namespace Orders.Application.Commands.CreateOrder;

public sealed record CreateOrderLineDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineDto> Lines) : IRequest<Result<Guid>>;
