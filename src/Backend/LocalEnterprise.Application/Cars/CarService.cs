using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Domain.Cars;

namespace LocalEnterprise.Application.Cars;

public sealed class CarService : ICarService
{
    private readonly ICarRepository _repository;

    public CarService(ICarRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CarDto>> ListAsync(CancellationToken cancellationToken)
    {
        var cars = await _repository.ListAsync(cancellationToken);
        return cars.Select(Map).ToList();
    }

    public async Task<CarDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var car = await _repository.GetByIdAsync(id, cancellationToken);
        return car is null ? null : Map(car);
    }

    public async Task<(bool Succeeded, string? Error, CarDto? Car)> CreateAsync(CreateCarRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByVinAsync(request.Vin.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null)
        {
            return (false, "A car with this VIN already exists.", null);
        }

        var car = Car.Create(request.Make, request.Model, request.Year, request.Vin);
        await _repository.CreateAsync(car, cancellationToken);

        return (true, null, Map(car));
    }

    public async Task<(bool Succeeded, string? Error, CarDto? Car)> UpdateAsync(Guid id, UpdateCarRequest request, CancellationToken cancellationToken)
    {
        var current = await _repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return (false, "Car not found.", null);
        }

        var duplicateByVin = await _repository.GetByVinAsync(request.Vin.Trim().ToUpperInvariant(), cancellationToken);
        if (duplicateByVin is not null && duplicateByVin.Id != id)
        {
            return (false, "A car with this VIN already exists.", null);
        }

        current.Update(request.Make, request.Model, request.Year, request.Vin);
        var updated = await _repository.UpdateAsync(current, cancellationToken);
        if (!updated)
        {
            return (false, "Car not found.", null);
        }

        return (true, null, Map(current));
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }

    private static CarDto Map(Car car)
    {
        return new CarDto(car.Id, car.Make, car.Model, car.Year, car.Vin);
    }
}
