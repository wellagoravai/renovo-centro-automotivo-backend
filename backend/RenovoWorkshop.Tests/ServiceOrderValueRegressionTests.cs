using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// O orçamento mandado ao cliente (WhatsAppServiceTests) só é confiável se
// ServiceOrder.Value continuar sempre igual a LaborValue + soma dos itens —
// estes testes cobrem o recálculo feito em ServiceOrdersController (Update,
// AddItem, RemoveItem) ponta a ponta, contra a API real.
public class ServiceOrderValueRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ServiceOrderValueRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid orderId, HttpClient client)> CreateOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();

        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Teste", WhatsApp = "5511999998888" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"TST{Guid.NewGuid():N}"[..7], CustomerId = customer.Id };
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = $"OS-TESTE-{Guid.NewGuid():N}",
            Status = "Em diagnóstico",
            CustomerId = customer.Id,
            VehicleId = vehicle.Id
        };
        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        return (order.Id, client);
    }

    private async Task<Guid> CreateInventoryItemAsync(decimal saleValue)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var item = new InventoryItem { Id = Guid.NewGuid(), Code = $"P-{Guid.NewGuid():N}", Description = "Peça de teste", Quantity = 100, SaleValue = saleValue };
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task UpdatingLaborValue_RecalculatesTotalValue()
    {
        var (orderId, client) = await CreateOrderAsync();

        var response = await client.PutAsJsonAsync($"/api/service-orders/{orderId}", new
        {
            diagnosis = "Diagnóstico teste",
            services = "Troca de óleo",
            parts = "",
            oils = "",
            filters = "",
            estimatedTime = 1,
            laborValue = 200m,
            notes = ""
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderValueResponse>();
        Assert.Equal(200m, body!.Value);
        Assert.Equal(200m, body.LaborValue);
    }

    [Fact]
    public async Task AddingAndRemovingItems_KeepsValueConsistentWithLaborPlusItems()
    {
        var (orderId, client) = await CreateOrderAsync();
        await client.PutAsJsonAsync($"/api/service-orders/{orderId}", new { diagnosis = "", services = "", parts = "", oils = "", filters = "", estimatedTime = 0, laborValue = 100m, notes = "" });

        var inventoryItemId = await CreateInventoryItemAsync(saleValue: 50m);
        var addResponse = await client.PostAsJsonAsync($"/api/service-orders/{orderId}/items", new { inventoryItemId, quantity = 3 });
        addResponse.EnsureSuccessStatusCode();
        var afterAdd = await addResponse.Content.ReadFromJsonAsync<OrderValueResponse>();

        // 100 (mão de obra) + 3 * 50 (peças) = 250
        Assert.Equal(250m, afterAdd!.Value);

        var itemId = afterAdd.Items.Single().Id;
        var removeResponse = await client.DeleteAsync($"/api/service-orders/{orderId}/items/{itemId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/service-orders/{orderId}");
        var afterRemove = await getResponse.Content.ReadFromJsonAsync<OrderValueResponse>();
        Assert.Equal(100m, afterRemove!.Value);
    }

    private sealed class OrderValueResponse
    {
        public decimal Value { get; set; }
        public decimal LaborValue { get; set; }
        public List<ItemResponse> Items { get; set; } = new();
    }

    private sealed class ItemResponse
    {
        public Guid Id { get; set; }
    }
}
