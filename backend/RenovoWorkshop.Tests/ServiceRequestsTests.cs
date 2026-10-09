using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Pedido (rascunho de OS de oficina): salvar o primeiro contato, aprovar/recusar e, aprovado,
// abrir a OS com os dados e as fotos do pedido.
public class ServiceRequestsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ServiceRequestsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record PhotoResponse(Guid Id, string Url);

    private record RequestResponse(
        Guid Id,
        string Number,
        string Status,
        string CustomerName,
        string VehiclePlate,
        string ProblemReported,
        Guid? ServiceOrderId,
        string? ServiceOrderNumber,
        int PhotoCount,
        List<PhotoResponse> Photos);

    private record OrderResponse(Guid Id, string Number, string Status, string ServiceType, string CustomerName, string VehiclePlate, string ProblemReported);

    private static string UniquePlate() => $"PD{Guid.NewGuid():N}"[..7].ToUpperInvariant();

    private static object Body(string? name = "Cliente Pedido", string? plate = null, string? document = null) => new
    {
        customerName = name,
        customerDocument = document,
        customerPhone = "(18) 99999-0000",
        vehiclePlate = plate ?? UniquePlate(),
        vehicleBrand = "Fiat",
        vehicleModel = "Uno",
        vehicleYear = 2015,
        problemReported = "Barulho na suspensão",
    };

    private static async Task<RequestResponse> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/service-requests", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RequestResponse>())!;
    }

    private async Task SeedPhotosAsync(Guid requestId, params string[] urls)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        foreach (var url in urls)
            context.ServiceRequestPhotos.Add(new ServiceRequestPhoto { Id = Guid.NewGuid(), ServiceRequestId = requestId, Url = url, UploadedBy = "teste" });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_WithOnlyPlate_IsSavedAsPending()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Mechanic);

        var created = await CreateAsync(client, Body(name: null));

        Assert.StartsWith("PED-", created.Number);
        Assert.Equal(ServiceRequestStatuses.Pendente, created.Status);
    }

    [Fact]
    public async Task Create_WithoutNameAndPlate_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/service-requests", Body(name: "", plate: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_BeforeApproval_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());

        var response = await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApproveThenCreateOrder_OpensOrderWithDataAndMovesPhotos()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var plate = UniquePlate();
        var created = await CreateAsync(client, Body(plate: plate));
        await SeedPhotosAsync(created.Id, "https://example.com/pedido-1.jpg", "https://example.com/pedido-2.jpg");

        var approved = await client.PostAsync($"/api/service-requests/{created.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var response = await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderResponse>())!;

        Assert.Equal("Cliente Pedido", order.CustomerName);
        Assert.Equal(plate, order.VehiclePlate);
        Assert.Equal("Barulho na suspensão", order.ProblemReported);
        Assert.Equal("Oficina", order.ServiceType);
        Assert.Equal("Recebido", order.Status);

        var orderPhotos = await client.GetFromJsonAsync<List<PhotoResponse>>($"/api/service-orders/{order.Id}/photos");
        Assert.Equal(2, orderPhotos!.Count);

        var request = await client.GetFromJsonAsync<RequestResponse>($"/api/service-requests/{created.Id}");
        Assert.Equal(ServiceRequestStatuses.OsCriada, request!.Status);
        Assert.Equal(order.Id, request.ServiceOrderId);
        Assert.Equal(order.Number, request.ServiceOrderNumber);
        Assert.Empty(request.Photos);
    }

    [Fact]
    public async Task CreateOrder_Twice_ReturnsConflictAndDoesNotDuplicate()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());
        await client.PostAsync($"/api/service-requests/{created.Id}/approve", null);

        var first = await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });
        var second = await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDataFails_KeepsRequestApprovedWithPhotos()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        // Oficina sem placa: a abertura da OS é recusada e nada pode ter mudado no pedido.
        var created = await CreateAsync(client, new { customerName = "Sem Placa", vehiclePlate = "" });
        await SeedPhotosAsync(created.Id, "https://example.com/pedido-3.jpg");
        await client.PostAsync($"/api/service-requests/{created.Id}/approve", null);

        var response = await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var request = await client.GetFromJsonAsync<RequestResponse>($"/api/service-requests/{created.Id}");
        Assert.Equal(ServiceRequestStatuses.Aprovado, request!.Status);
        Assert.Single(request.Photos);
    }

    [Fact]
    public async Task Update_AfterOrderCreated_ReturnsConflict()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());
        await client.PostAsync($"/api/service-requests/{created.Id}/approve", null);
        await client.PostAsJsonAsync($"/api/service-orders/from-request/{created.Id}", new { responsibleUser = "Teste" });

        var response = await client.PutAsJsonAsync($"/api/service-requests/{created.Id}", Body());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CustomerUpdate_AcceptsEmptyDocument()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
            context.Customers.Add(new Customer { Id = Guid.Parse("7f3e0d7a-0000-4000-8000-000000000001"), Name = "Avulso" });
            await context.SaveChangesAsync();
        }
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PutAsJsonAsync("/api/Customers/7f3e0d7a-0000-4000-8000-000000000001", new
        {
            name = "Avulso Corrigido",
            document = "",
            phone = "(18) 98888-0000",
            whatsApp = "",
            email = "",
            address = "",
            notes = "",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
