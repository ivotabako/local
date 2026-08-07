using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Application.Cars;
using LocalEnterprise.Domain.Cars;

namespace LocalEnterprise.Tests.Unit;

public class CarServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenVinAlreadyExists()
    {
        var repository = new InMemoryCarRepository();
        await repository.CreateAsync(Car.Create("Ford", "Mustang", 2020, "VIN-1"), CancellationToken.None);

        var service = new CarService(repository);

        var result = await service.CreateAsync(new CreateCarRequest("BMW", "M3", 2022, "VIN-1"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("A car with this VIN already exists.", result.Error);
    }

    private sealed class InMemoryCarRepository : ICarRepository
    {
        private readonly List<Car> _cars = [];

        public Task<IReadOnlyList<Car>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Car>>(_cars.ToList());
        }

        public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_cars.FirstOrDefault(x => x.Id == id));
        }

        public Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken)
        {
            var normalized = vin.Trim().ToUpperInvariant();
            return Task.FromResult(_cars.FirstOrDefault(x => x.Vin == normalized));
        }

        public Task CreateAsync(Car car, CancellationToken cancellationToken)
        {
            _cars.Add(car);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(Car car, CancellationToken cancellationToken)
        {
            var index = _cars.FindIndex(x => x.Id == car.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _cars[index] = car;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var removed = _cars.RemoveAll(x => x.Id == id) > 0;
            return Task.FromResult(removed);
        }
    }
}
