using LocalEnterprise.Domain.Abstractions;

namespace LocalEnterprise.Domain.Cars;

public sealed class Car : Entity, IAggregateRoot
{
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Vin { get; private set; } = string.Empty;

    public static Car Create(string make, string model, int year, string vin)
    {
        return new Car
        {
            Make = NormalizeRequired(make, nameof(make)),
            Model = NormalizeRequired(model, nameof(model)),
            Year = ValidateYear(year),
            Vin = NormalizeRequired(vin, nameof(vin)).ToUpperInvariant()
        };
    }

    public static Car Rehydrate(Guid id, string make, string model, int year, string vin)
    {
        return new Car
        {
            Id = id,
            Make = NormalizeRequired(make, nameof(make)),
            Model = NormalizeRequired(model, nameof(model)),
            Year = ValidateYear(year),
            Vin = NormalizeRequired(vin, nameof(vin)).ToUpperInvariant()
        };
    }

    public void Update(string make, string model, int year, string vin)
    {
        Make = NormalizeRequired(make, nameof(make));
        Model = NormalizeRequired(model, nameof(model));
        Year = ValidateYear(year);
        Vin = NormalizeRequired(vin, nameof(vin)).ToUpperInvariant();
    }

    private static int ValidateYear(int year)
    {
        var currentYear = DateTime.UtcNow.Year + 1;
        if (year < 1886 || year > currentYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year), $"Year must be between 1886 and {currentYear}.");
        }

        return year;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
