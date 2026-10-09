using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Menu "Orçamentos" (/api/quotes): montar o orçamento da OS (peças do estoque e
// avulsas, mão de obra, desconto, fotos), gerar o PDF e enviar ao cliente.
public class QuotesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public QuotesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid orderId, Guid inventoryItemId, Guid photoId)> SeedAsync(string whatsApp = "5518999990000", string status = "Em diagnóstico")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();

        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Orçamento", WhatsApp = whatsApp };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"Q{Guid.NewGuid():N}"[..7], Model = "Onix", CustomerId = customer.Id };
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = $"OS-ORC-{Guid.NewGuid():N}",
            Status = status,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id
        };
        var inventoryItem = new InventoryItem { Id = Guid.NewGuid(), Code = $"P-{Guid.NewGuid():N}", Description = "Filtro de óleo", Quantity = 10, SaleValue = 40m };
        var photo = new ServiceOrderPhoto { Id = Guid.NewGuid(), ServiceOrderId = order.Id, Url = "https://example.invalid/placa.jpg" };

        context.AddRange(customer, vehicle, order, inventoryItem, photo);
        await context.SaveChangesAsync();
        return (order.Id, inventoryItem.Id, photo.Id);
    }

    private HttpClient Client() => _factory.CreateAuthorizedClient(UserRoles.Administrator);

    [Fact]
    public async Task Save_WithStockAndLooseParts_LaborAndDiscount_RecalculatesTotal()
    {
        var (orderId, inventoryItemId, photoId) = await SeedAsync();
        var client = Client();

        var response = await client.PutAsJsonAsync($"/api/quotes/{orderId}", new
        {
            diagnosis = "Freios gastos",
            services = "Troca de pastilhas",
            laborValue = 120m,
            discount = 20m,
            vehiclePhotoId = photoId,
            partPhotoIds = Array.Empty<Guid>(),
            items = new object[]
            {
                new { inventoryItemId, quantity = 2, unitValue = 45m },
                new { description = "Pastilha dianteira", quantity = 1, unitValue = 180m }
            }
        });

        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteDetailDto>();
        // 120 + 2 x 45 + 180 − 20
        Assert.Equal(370m, quote!.Total);
        Assert.Equal(270m, quote.PartsTotal);
        Assert.Equal(photoId, quote.VehiclePhotoId);
        Assert.Contains(quote.Items, i => i.InventoryItemId is null && i.Description == "Pastilha dianteira");

        // Reenviar a lista sem a peça avulsa remove o item.
        var stockItem = quote.Items.Single(i => i.InventoryItemId == inventoryItemId);
        var second = await client.PutAsJsonAsync($"/api/quotes/{orderId}", new
        {
            laborValue = 120m,
            discount = 0m,
            partPhotoIds = Array.Empty<Guid>(),
            items = new object[] { new { id = stockItem.Id, inventoryItemId, quantity = 3, unitValue = 45m } }
        });
        second.EnsureSuccessStatusCode();
        var updated = await second.Content.ReadFromJsonAsync<QuoteDetailDto>();
        Assert.Single(updated!.Items);
        Assert.Equal(255m, updated.Total);
        Assert.Equal("Freios gastos", updated.Diagnosis);
    }

    [Fact]
    public async Task Save_RejectsDiscountGreaterThanQuote()
    {
        var (orderId, _, _) = await SeedAsync();

        var response = await Client().PutAsJsonAsync($"/api/quotes/{orderId}", new
        {
            laborValue = 50m,
            discount = 60m,
            partPhotoIds = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_ReturnsPdfFile()
    {
        var (orderId, _, _) = await SeedAsync();
        var client = Client();
        await client.PutAsJsonAsync($"/api/quotes/{orderId}", new { laborValue = 100m, discount = 10m, partPhotoIds = Array.Empty<Guid>(), items = new object[] { new { description = "Correia", quantity = 1, unitValue = 90m } } });

        var response = await client.GetAsync($"/api/quotes/{orderId}/pdf");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Send_MovesOrderToAwaitingApproval_AndRecordsHistory()
    {
        var (orderId, _, _) = await SeedAsync();
        var client = Client();
        await client.PutAsJsonAsync($"/api/quotes/{orderId}", new { laborValue = 100m, discount = 0m, partPhotoIds = Array.Empty<Guid>() });

        var response = await client.PostAsync($"/api/quotes/{orderId}/send", null);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SendQuoteResultDto>();
        Assert.True(result!.Success);
        Assert.Equal(ServiceOrderStatuses.AguardandoAprovacao, result.Status);
        Assert.NotNull(result.QuoteSentAt);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        Assert.True(await context.ServiceOrderHistories.AnyAsync(h => h.ServiceOrderId == orderId && h.Status == ServiceOrderStatuses.AguardandoAprovacao));
    }

    [Fact]
    public async Task Send_KeepsStatus_WhenOrderIsAlreadyPastApproval()
    {
        var (orderId, _, _) = await SeedAsync(status: "Em manutenção");
        var client = Client();
        await client.PutAsJsonAsync($"/api/quotes/{orderId}", new { laborValue = 100m, discount = 0m, partPhotoIds = Array.Empty<Guid>() });

        var response = await client.PostAsync($"/api/quotes/{orderId}/send", null);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SendQuoteResultDto>();
        Assert.Equal("Em manutenção", result!.Status);
    }

    [Fact]
    public async Task Send_ReturnsBadRequest_WhenCustomerHasNoPhone()
    {
        var (orderId, _, _) = await SeedAsync(whatsApp: "");
        var client = Client();
        await client.PutAsJsonAsync($"/api/quotes/{orderId}", new { laborValue = 100m, discount = 0m, partPhotoIds = Array.Empty<Guid>() });

        var response = await client.PostAsync($"/api/quotes/{orderId}/send", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Peça avulsa não tem saldo em estoque: a baixa ao entregar a OS só debita os itens do estoque.
    [Fact]
    public async Task Delivering_OrderWithLoosePart_DeductsOnlyStockItems()
    {
        var (orderId, inventoryItemId, _) = await SeedAsync(status: "Testes");
        var client = Client();
        await client.PutAsJsonAsync($"/api/quotes/{orderId}", new
        {
            laborValue = 0m,
            discount = 0m,
            partPhotoIds = Array.Empty<Guid>(),
            items = new object[]
            {
                new { inventoryItemId, quantity = 2, unitValue = 40m },
                new { description = "Peça encomendada", quantity = 1, unitValue = 100m }
            }
        });

        var response = await client.PatchAsJsonAsync($"/api/service-orders/{orderId}/status", new { status = "Entregue", changedBy = "Teste" });
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var inventoryItem = await context.InventoryItems.AsNoTracking().FirstAsync(i => i.Id == inventoryItemId);
        Assert.Equal(8, inventoryItem.Quantity);
    }
}
