using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Abertura de OS pelo app da equipe (POST with-customer-vehicle): no guincho o chamado
// é aberto no escritório sem os dados do veículo (coletados no local da remoção), nasce
// no fluxo de status do guincho e um CPF ainda não cadastrado não bloqueia a urgência.
public class TowOpeningFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TowOpeningFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static object Request(string serviceType, string plate, string document = "", string name = "Cliente Chamado") => new
    {
        serviceType,
        problemReported = "Remoção",
        responsibleUser = "Equipe",
        customer = new { name, document, phone = "(18) 99999-0000" },
        vehicle = new { plate, brand = "", model = "", year = 0, color = "", mileage = 0 },
    };

    [Fact]
    public async Task Guincho_WithoutPlate_CreatesOrderInTowFlow()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle", Request(ServiceOrderTypes.Guincho, ""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Chamado recebido", body!.Status);
        Assert.Equal(string.Empty, body.VehiclePlate);
    }

    [Fact]
    public async Task Guincho_TwoOrdersWithoutPlate_DoNotShareVehicle()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var first = await (await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request(ServiceOrderTypes.Guincho, "", name: "Primeiro"))).Content.ReadFromJsonAsync<OrderResponse>();
        var second = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request(ServiceOrderTypes.Guincho, "", name: "Segundo"));

        // Sem placa não há busca: o segundo chamado não cai no "placa já vinculada a outro cliente".
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotEqual(first!.VehicleId, secondBody!.VehicleId);
    }

    [Fact]
    public async Task Oficina_WithoutPlate_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle", Request(ServiceOrderTypes.Oficina, " "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oficina_KeepsDefaultStatusRecebido()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request(ServiceOrderTypes.Oficina, $"OF{Guid.NewGuid():N}"[..7]));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Recebido", body!.Status);
    }

    [Fact]
    public async Task Guincho_WithUnregisteredDocument_CreatesCustomerInsteadOfBlocking()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        const string cpf = "172.381.208-01";

        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request(ServiceOrderTypes.Guincho, "", cpf, "José Pereira Da Silva"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var customer = await context.Customers.SingleAsync(c => c.Document == "17238120801");
        Assert.Equal("José Pereira Da Silva", customer.Name);
        Assert.True(await context.DashboardNotifications.AnyAsync(n => n.CustomerId == customer.Id && n.Type == "customer-registration"));
    }

    [Fact]
    public async Task Oficina_WithUnregisteredDocument_StillRequiresRegistration()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request(ServiceOrderTypes.Oficina, $"OF{Guid.NewGuid():N}"[..7], "529.982.247-25"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Cliente pediu para aguardar no posto")]
    public async Task StatusChange_NoteGoesToHistoryOnly_KeepsOrderNotes(string statusNote)
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await (await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle", new
        {
            serviceType = ServiceOrderTypes.Guincho,
            notes = "Orçamento aprovado: R$ 1.034,00",
            customer = new { name = "Cliente Acordo" },
            vehicle = new { plate = "" },
        })).Content.ReadFromJsonAsync<OrderResponse>();

        var patch = await client.PatchAsJsonAsync($"/api/service-orders/{created!.Id}/status",
            new { status = "A caminho do local", notes = statusNote, changedBy = "Equipe" });
        patch.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var order = await context.ServiceOrders.SingleAsync(o => o.Id == created.Id);
        Assert.Equal("A caminho do local", order.Status);
        Assert.Equal("Orçamento aprovado: R$ 1.034,00", order.Notes);

        var history = await context.ServiceOrderHistories
            .Where(h => h.ServiceOrderId == created.Id && h.Status == "A caminho do local")
            .SingleAsync();
        Assert.Equal(statusNote == "" ? "Status alterado de Chamado recebido para A caminho do local" : statusNote, history.Notes);
    }

    private record OrderResponse(Guid Id, string Status, string VehiclePlate, Guid VehicleId);
}
