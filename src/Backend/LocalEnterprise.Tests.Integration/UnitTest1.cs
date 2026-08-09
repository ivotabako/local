namespace LocalEnterprise.Tests.Integration;

public class UnitTest1
{
    [Fact]
    public void ApiAssemblyLoads()
    {
        var assembly = typeof(LocalEnterprise.Api.Security.JwtOptions).Assembly;
        Assert.NotNull(assembly);
    }
}
