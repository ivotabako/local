using LocalEnterprise.Application.Abstractions;
using LocalEnterprise.Domain.Cars;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace LocalEnterprise.Infrastructure.Persistence.Cars;

public sealed class MongoCarRepository : ICarRepository
{
    private readonly IMongoCollection<CarDocument> _cars;

    public MongoCarRepository(IMongoDatabase database)
    {
        _cars = database.GetCollection<CarDocument>("cars");

        var vinIndex = new CreateIndexModel<CarDocument>(
            Builders<CarDocument>.IndexKeys.Ascending(x => x.Vin),
            new CreateIndexOptions { Unique = true, Name = "ux_cars_vin" });

        _cars.Indexes.CreateOne(vinIndex);
    }

    public async Task<IReadOnlyList<Car>> ListAsync(CancellationToken cancellationToken)
    {
        var docs = await _cars.Find(Builders<CarDocument>.Filter.Empty)
            .SortBy(x => x.Make)
            .ThenBy(x => x.Model)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToDomain).ToList();
    }

    public async Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var doc = await _cars.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : MapToDomain(doc);
    }

    public async Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken)
    {
        var normalizedVin = vin.Trim().ToUpperInvariant();
        var doc = await _cars.Find(x => x.Vin == normalizedVin).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : MapToDomain(doc);
    }

    public Task CreateAsync(Car car, CancellationToken cancellationToken)
    {
        return _cars.InsertOneAsync(MapToDocument(car), cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(Car car, CancellationToken cancellationToken)
    {
        var result = await _cars.ReplaceOneAsync(
            x => x.Id == car.Id,
            MapToDocument(car),
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cars.DeleteOneAsync(x => x.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private static CarDocument MapToDocument(Car car)
    {
        return new CarDocument
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Year = car.Year,
            Vin = car.Vin
        };
    }

    private static Car MapToDomain(CarDocument document)
    {
        return Car.Rehydrate(document.Id, document.Make, document.Model, document.Year, document.Vin);
    }

    private sealed record CarDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; init; }

        public required string Make { get; init; }
        public required string Model { get; init; }
        public int Year { get; init; }
        public required string Vin { get; init; }
    }
}
