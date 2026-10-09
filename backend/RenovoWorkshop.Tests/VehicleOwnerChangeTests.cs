using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Carro que já passou pela oficina com outro cliente: o check-in abre a OS no nome do
// cliente novo (o veículo muda de dono) sem mexer nas OS antigas. E a troca de status
// só aceita os status que o painel mostra.
public class VehicleOwnerChangeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VehicleOwnerChangeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static object Request(string plate, string name) => new
    {
        serviceType = ServiceOrderTypes.Oficina,
        problemReported = "Bomba d'água",
        responsibleUser = "Equipe",
        customer = new { name, document = "", phone = "(18) 99999-0000" },
        vehicle = new { plate, brand = "Hyundai", model = "HB20", year = 2015, color = "Branca", mileage = 66509 },
    };

    [Fact]
    public async Task CheckIn_PlateOfAnotherCustomer_OpensOrderForNewCustomerAndKeepsOldOrder()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var plate = $"HB{Guid.NewGuid():N}"[..7].ToUpperInvariant();

        var first = await (await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle", Request(plate, "Dono Antigo")))
            .Content.ReadFromJsonAsync<OrderResponse>();
        var response = await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle", Request(plate, "Dono Novo"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var second = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Dono Novo", second!.CustomerName);
        Assert.Equal(first!.VehicleId, second.VehicleId);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var oldOrder = await context.ServiceOrders.Include(o => o.Customer).SingleAsync(o => o.Id == first.Id);
        Assert.Equal("Dono Antigo", oldOrder.Customer.Name);
        var vehicle = await context.Vehicles.SingleAsync(v => v.Id == second.VehicleId);
        Assert.Equal(second.CustomerId, vehicle.CustomerId);
        Assert.Contains(await context.ServiceOrderHistories.Where(h => h.ServiceOrderId == second.Id).Select(h => h.Notes).ToListAsync(),
            n => n.Contains("Dono Antigo"));
    }

    [Fact]
    public async Task StatusChange_StatusOutsideDashboard_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await (await client.PostAsJsonAsync("/api/service-orders/with-customer-vehicle",
            Request($"ST{Guid.NewGuid():N}"[..7].ToUpperInvariant(), "Cliente Status"))).Content.ReadFromJsonAsync<OrderResponse>();

        var invalid = await client.PatchAsJsonAsync($"/api/service-orders/{created!.Id}/status",
            new { status = "Em andamento", changedBy = "Equipe" });
        var valid = await client.PatchAsJsonAsync($"/api/service-orders/{created.Id}/status",
            new { status = "Em manutenção", changedBy = "Equipe" });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    private record OrderResponse(Guid Id, Guid VehicleId, Guid CustomerId, string CustomerName);
}
