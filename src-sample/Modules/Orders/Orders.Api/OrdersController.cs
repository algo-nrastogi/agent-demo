using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Commands.CancelOrder;
using Orders.Application.Commands.CreateOrder;

namespace Orders.Api;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            request.Lines.Select(l => new CreateOrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList());

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), new { id = result.Value }, result.Value)
            : BadRequest(new { error = result.Error.Message });

    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(orderId, request.CustomerId), cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error.Code switch
        {
            "NotFound" => NotFound(new { error = result.Error.Message }),
            "Forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error.Message }),
            "Conflict" => Conflict(new { error = result.Error.Message }),
            _ => BadRequest(new { error = result.Error.Message })
        };
    }
}

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderLineRequest> Lines);
public sealed record CreateOrderLineRequest(string Sku, int Quantity, decimal UnitPrice);
public sealed record CancelOrderRequest(Guid CustomerId);
