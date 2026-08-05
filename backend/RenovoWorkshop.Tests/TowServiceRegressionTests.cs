using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Cobre o pedido central desta fase: uma OS só serve os dois fluxos (Oficina e
// Guincho), a conversão Guincho -> Oficina preserva cliente/veículo/histórico, e o
// dashboard de guincho conta pelos status certos (não pelos de oficina).
public class TowServiceRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TowServiceRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid customerId, Guid vehicleId)> SeedCustomerAndVehicleAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Guincho", WhatsApp = "5511999998888" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"TW{Guid.NewGuid():N}"[..7], CustomerId = customer.Id };
        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        return (customer.Id, vehicle.Id);
    }

    [Fact]
    public async Task CreateOrder_WithGuinchoType_PersistsTowDetails()
    {
        var (customerId, vehicleId) = await SeedCustomerAndVehicleAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-orders", new
        {
            serviceType = ServiceOrderTypes.Guincho,
            problemReported = "Veículo quebrado na rodovia",
            status = "Chamado recebido",
            customerId,
            vehicleId,
            towDetails = new
            {
                insuranceCompany = "Seguradora Teste",
                claimNumber = "SIN-123",
                pickupLocation = "Km 45 da rodovia",
                deliveryDestination = "Renovo Centro Automotivo"
            }
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(ServiceOrderTypes.Guincho, body!.ServiceType);
        Assert.NotNull(body.TowDetails);
        Assert.Equal("Seguradora Teste", body.TowDetails!.InsuranceCompany);
        Assert.Equal("SIN-123", body.TowDetails.ClaimNumber);
    }

    [Fact]
    public async Task ConvertToOficina_ChangesTypeAndStatus_KeepingCustomerAndVehicle()
    {
        var (customerId, vehicleId) = await SeedCustomerAndVehicleAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var createResponse = await client.PostAsJsonAsync("/api/service-orders", new
        {
            serviceType = ServiceOrderTypes.Guincho,
            problemReported = "Pane elétrica",
            status = "Em transporte",
            customerId,
            vehicleId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var convertResponse = await client.PatchAsJsonAsync($"/api/service-orders/{created!.Id}/convert-to-oficina", new { changedBy = "Recepção" });
        convertResponse.EnsureSuccessStatusCode();
        var converted = await convertResponse.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(ServiceOrderTypes.Oficina, converted!.ServiceType);
        Assert.Equal(ServiceOrderStatuses.Oficina[0], converted.Status);
        Assert.Equal(customerId, converted.CustomerId);
        Assert.Equal(vehicleId, converted.VehicleId);
    }

    [Fact]
    public async Task ConvertToOficina_OnAlreadyOficinaOrder_ReturnsBadRequest()
    {
        var (customerId, vehicleId) = await SeedCustomerAndVehicleAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var createResponse = await client.PostAsJsonAsync("/api/service-orders", new
        {
            serviceType = ServiceOrderTypes.Oficina,
            problemReported = "Revisão",
            status = "Recebido",
            customerId,
            vehicleId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var convertResponse = await client.PatchAsJsonAsync($"/api/service-orders/{created!.Id}/convert-to-oficina", new { changedBy = "Recepção" });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, convertResponse.StatusCode);
    }

    [Fact]
    public async Task TowDashboard_CountsOnlyGuinchoOrders_ByTowStatuses()
    {
        var (customerId, vehicleId) = await SeedCustomerAndVehicleAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        // Um Guincho "Em transporte" e um Oficina "Em diagnóstico" — só o primeiro deve aparecer no dashboard de guincho.
        await client.PostAsJsonAsync("/api/service-orders", new { serviceType = ServiceOrderTypes.Guincho, problemReported = "x", status = "Em transporte", customerId, vehicleId });
        await client.PostAsJsonAsync("/api/service-orders", new { serviceType = ServiceOrderTypes.Oficina, problemReported = "y", status = "Em diagnóstico", customerId, vehicleId });

        var response = await client.GetAsync("/api/dashboard/tow");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TowDashboardResponse>();

        Assert.True(body!.InTransport >= 1);
    }

    [Fact]
    public async Task Checklist_PersistsNewAccessoryFieldsAndDamagePoints()
    {
        var (customerId, vehicleId) = await SeedCustomerAndVehicleAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var orderResponse = await client.PostAsJsonAsync("/api/service-orders", new { serviceType = ServiceOrderTypes.Guincho, problemReported = "z", status = "Chamado recebido", customerId, vehicleId });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var checklistResponse = await client.PostAsJsonAsync("/api/checklists", new
        {
            vehicleId,
            serviceOrderId = order!.Id,
            fuelLevel = "1/2",
            antenna = true,
            hubcaps = true,
            radio = true,
            damagePoints = "front_left,rear_bumper"
        });

        checklistResponse.EnsureSuccessStatusCode();
        var checklist = await checklistResponse.Content.ReadFromJsonAsync<ChecklistResponse>();

        Assert.True(checklist!.Antenna);
        Assert.True(checklist.Hubcaps);
        Assert.True(checklist.Radio);
        Assert.Equal("front_left,rear_bumper", checklist.DamagePoints);
    }

    private sealed class OrderResponse
    {
        public Guid Id { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public TowDetailsResponse? TowDetails { get; set; }
    }

    private sealed class TowDetailsResponse
    {
        public string InsuranceCompany { get; set; } = string.Empty;
        public string ClaimNumber { get; set; } = string.Empty;
    }

    private sealed class TowDashboardResponse
    {
        public int InTransport { get; set; }
    }

    private sealed class ChecklistResponse
    {
        public bool Antenna { get; set; }
        public bool Hubcaps { get; set; }
        public bool Radio { get; set; }
        public string DamagePoints { get; set; } = string.Empty;
    }
}
