namespace Orders.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public sealed class OrderLine
{
    public string Sku { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    public OrderLine(string sku, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed class Order
{
    private readonly List<OrderLine> _lines = new();

    private Order(Guid id, Guid customerId, DateTime placedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        PlacedAtUtc = placedAtUtc;
        Status = OrderStatus.Pending;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public DateTime PlacedAtUtc { get; }
    public OrderStatus Status { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    /// <summary>
    /// Domain invariant: an order must be created with at least one line item.
    /// Enforced here so it is impossible to construct an invalid Order anywhere in the codebase.
    /// </summary>
    public static Order Create(Guid customerId, IReadOnlyCollection<OrderLine> lines, DateTime placedAtUtc)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("An order must contain at least one line item.");

        var order = new Order(Guid.NewGuid(), customerId, placedAtUtc);
        order._lines.AddRange(lines);
        return order;
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new InvalidOperationException($"Order cannot be cancelled when status is {Status}.");

        if (cancelledAtUtc == default)
            throw new ArgumentException("Cancelled at timestamp is required.", nameof(cancelledAtUtc));

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }
}
