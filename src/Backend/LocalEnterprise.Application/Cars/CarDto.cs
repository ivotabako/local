namespace LocalEnterprise.Application.Cars;

public sealed record CarDto(Guid Id, string Make, string Model, int Year, string Vin);
