using RenovoWorkshop.Domain.Constants;
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

    [Theory]
    [InlineData("Chamado recebido")]
    [InlineData("A caminho do local")]
    [InlineData("Veículo carregado")]
    [InlineData("Em transporte")]
    public void BuildStatusMessage_ShouldMentionOrderAndVehicle_ForGuinchoOnlyStatuses(string newStatus)
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder
        {
            Number = "OS-202608070001",
            ServiceType = ServiceOrderTypes.Guincho,
            Status = newStatus,
            Vehicle = new Vehicle { Plate = "ABC1234", Model = "Onix" }
        };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Chamado recebido", newStatus);

        Assert.Contains("OS-202608070001", message);
        Assert.Contains("Maria Silva", message);
        Assert.Contains("ABC1234", message);
        Assert.Contains("Onix", message);
    }

    [Fact]
    public void BuildStatusMessage_Entregue_UsesGuinchoWording_WhenServiceTypeIsGuincho()
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder { Number = "OS-202608070002", ServiceType = ServiceOrderTypes.Guincho, Status = "Entregue" };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Em transporte", "Entregue");

        Assert.Contains("entregue com sucesso no destino combinado", message);
    }

    [Fact]
    public void BuildStatusMessage_Entregue_FallsBackToGenericWording_WhenServiceTypeIsOficina()
    {
        // OS de oficina não tem branch dedicado pra "Entregue" (esse status já existia antes do
        // guincho) — cai no fallback genérico. Este teste trava esse comportamento pré-existente,
        // pra um branch futuro só de guincho não acabar "vazando" pra oficina sem querer.
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder { Number = "OS-202608070002", ServiceType = ServiceOrderTypes.Oficina, Status = "Entregue" };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "Pronto para retirada", "Entregue");

        Assert.Contains("OS-202608070002", message);
        Assert.DoesNotContain("destino combinado", message);
    }

    [Fact]
    public void BuildStatusMessage_ShouldOmitVehicleLabel_WhenPlateIsMissing()
    {
        var service = new WhatsAppService(null!, null!);
        var order = new ServiceOrder
        {
            Number = "OS-202608070003",
            ServiceType = ServiceOrderTypes.Guincho,
            Status = "Chamado recebido",
            Vehicle = new Vehicle { Plate = "", Model = "Onix" }
        };
        var customer = new Customer { Name = "Maria Silva" };

        var message = service.BuildStatusMessage(order, customer, "", "Chamado recebido");

        Assert.DoesNotContain("Onix", message);
    }
}
