using LocalEnterprise.Domain.Cars;

namespace LocalEnterprise.Application.Abstractions;

public interface ICarRepository
{
    Task<IReadOnlyList<Car>> ListAsync(CancellationToken cancellationToken);
    Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken);
    Task CreateAsync(Car car, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Car car, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
