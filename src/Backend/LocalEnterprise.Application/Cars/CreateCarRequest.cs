namespace LocalEnterprise.Application.Cars;

public sealed record CreateCarRequest(string Make, string Model, int Year, string Vin);
