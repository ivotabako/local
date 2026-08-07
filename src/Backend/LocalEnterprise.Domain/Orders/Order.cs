using LocalEnterprise.Domain.Abstractions;

namespace LocalEnterprise.Domain.Orders;

public sealed class Order : Entity, IAggregateRoot
{
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}
