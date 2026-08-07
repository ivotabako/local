using LocalEnterprise.Domain.Cars;

namespace LocalEnterprise.Tests.Unit;

public class CarsDomainTests
{
    [Fact]
    public void Create_NormalizesStringsAndVin()
    {
        var car = Car.Create("  Volvo  ", " xc90 ", 2024, " wa1abc123 ");

        Assert.Equal("Volvo", car.Make);
        Assert.Equal("xc90", car.Model);
        Assert.Equal("WA1ABC123", car.Vin);
    }

    [Fact]
    public void Create_Throws_WhenYearOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Car.Create("Volvo", "XC90", 1700, "VIN123"));
    }
}
