namespace LocalEnterprise.Application.Cars;

public interface ICarService
{
    Task<IReadOnlyList<CarDto>> ListAsync(CancellationToken cancellationToken);
    Task<CarDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? Error, CarDto? Car)> CreateAsync(CreateCarRequest request, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? Error, CarDto? Car)> UpdateAsync(Guid id, UpdateCarRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
