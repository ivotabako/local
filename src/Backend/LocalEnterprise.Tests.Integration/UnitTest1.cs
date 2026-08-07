namespace LocalEnterprise.Tests.Integration;

public class UnitTest1
{
    [Fact]
    public void ApiAssemblyLoads()
    {
        var assembly = typeof(Program).Assembly;
        Assert.NotNull(assembly);
    }
}
