using LocalEnterprise.Domain.Orders;

namespace LocalEnterprise.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
