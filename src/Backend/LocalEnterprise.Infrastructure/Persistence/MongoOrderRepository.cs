using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Domain.Orders;
using MongoDB.Driver;

namespace LocalEnterprise.Infrastructure.Persistence;

public sealed class MongoOrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _orders;

    public MongoOrderRepository(IMongoDatabase database)
    {
        _orders = database.GetCollection<Order>("orders");
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var filter = Builders<Order>.Filter.Eq(x => x.Id, id);
        return await _orders.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        return await _orders.Find(Builders<Order>.Filter.Empty)
            .SortByDescending(x => x.CreatedUtc)
            .Limit(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }
}
