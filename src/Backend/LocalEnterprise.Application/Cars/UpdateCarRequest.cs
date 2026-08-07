namespace LocalEnterprise.Application.Cars;

public sealed record UpdateCarRequest(string Make, string Model, int Year, string Vin);
