namespace LocalEnterprise.Tests.Unit;

public class UnitTest1
{
    [Fact]
    public void DomainOrderHasDefaults()
    {
        var order = new LocalEnterprise.Domain.Orders.Order
        {
            CustomerId = "customer-1",
            TotalAmount = 42.50m
        };

        Assert.Equal("customer-1", order.CustomerId);
        Assert.True(order.TotalAmount > 0);
    }
}
