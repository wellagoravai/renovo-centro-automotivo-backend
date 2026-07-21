using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Services;

namespace RenovoWorkshop.Tests;

public class WhatsAppServiceTests
{
    [Fact]
    public void BuildStatusMessage_ShouldIncludeOrderNumberCustomerAndStatus()
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder
        {
            Number = "OS-202607081200",
            Status = "Em diagnóstico"
        };
        var customer = new Customer
        {
            Name = "Maria Silva"
        };

        var message = service.BuildStatusMessage(order, customer, "Recebido", "Em diagnóstico", "Aguardando análise");

        Assert.Contains("Maria Silva", message);
        Assert.Contains("OS-202607081200", message);
        Assert.Contains("Em diagnóstico", message);
    }
}
