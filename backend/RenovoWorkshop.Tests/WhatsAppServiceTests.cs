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

    [Fact]
    public void BuildStatusMessage_ShouldAskForSimOuNao_WhenAwaitingApproval()
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder { Number = "OS-202607081200", Status = "Aguardando aprovação" };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Em diagnóstico", "Aguardando aprovação");

        Assert.Contains("SIM", message);
        Assert.Contains("NÃO", message);
    }

    [Theory]
    [InlineData("Em manutenção")]
    [InlineData("Pronto para retirada")]
    public void BuildStatusMessage_ShouldMentionOrderNumber_ForOtherKnownStatuses(string newStatus)
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder { Number = "OS-202607081200", Status = newStatus };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Aprovado", newStatus);

        Assert.Contains("OS-202607081200", message);
        Assert.Contains("Maria Silva", message);
    }

    [Fact]
    public void BuildStatusMessage_ShouldIncludeDiagnosisPartsLaborAndTotal_WhenAwaitingApproval()
    {
        var service = new WhatsAppService(null!, null!);
        var inventoryItem = new InventoryItem { Description = "Amortecedor dianteiro" };
        var order = new ServiceOrder
        {
            Number = "OS-202607081200",
            Status = "Aguardando aprovação",
            Diagnosis = "Amortecedor dianteiro danificado",
            Services = "Troca de amortecedores",
            LaborValue = 200m,
            Value = 350m,
            Items = new List<ServiceOrderItem>
            {
                new() { Quantity = 1, UnitValue = 150m, InventoryItem = inventoryItem }
            }
        };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Em diagnóstico", "Aguardando aprovação");

        Assert.Contains("Amortecedor dianteiro danificado", message);
        Assert.Contains("Troca de amortecedores", message);
        Assert.Contains("Amortecedor dianteiro", message);
        Assert.Contains("200,00", message);
        Assert.Contains("350,00", message);
    }
}
