using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Custos de viagem do guincho: lançamento por categoria, valor em texto aceito com
// vírgula ou ponto, vínculo opcional com OS e permissão de escrita.
public class TripExpensesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TripExpensesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent Form(string category, string amount, string? description = null, Guid? serviceOrderId = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(category), "category" },
            { new StringContent(amount), "amount" },
        };
        if (description is not null) form.Add(new StringContent(description), "description");
        if (serviceOrderId.HasValue) form.Add(new StringContent(serviceOrderId.Value.ToString()), "serviceOrderId");
        return form;
    }

    private async Task<Guid> SeedTowOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Viagem" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"TV{Guid.NewGuid():N}"[..7], CustomerId = customer.Id };
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = $"OS-{Guid.NewGuid():N}"[..12],
            ServiceType = ServiceOrderTypes.Guincho,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
        };
        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        return order.Id;
    }

    [Fact]
    public async Task Create_WithCommaDecimalAndOrder_PersistsAndListsByOrder()
    {
        var orderId = await SeedTowOrderAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsync("/api/trip-expenses",
            Form(TripExpenseCategories.Pedagio, "23,40", "Praça de Castilho", orderId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.Equal(23.40m, created!.Amount);
        Assert.Equal(TripExpenseCategories.Pedagio, created.Category);
        Assert.Equal("Praça de Castilho", created.Description);
        Assert.Equal(orderId, created.ServiceOrderId);
        Assert.Null(created.PhotoUrl);

        var list = await client.GetFromJsonAsync<List<ExpenseResponse>>($"/api/trip-expenses?serviceOrderId={orderId}");
        Assert.Single(list!);
        Assert.Equal(created.Id, list![0].Id);
    }

    [Theory]
    [InlineData("Categoria inexistente", "10")]
    [InlineData(TripExpenseCategories.Abastecimento, "0")]
    [InlineData(TripExpenseCategories.Abastecimento, "abc")]
    public async Task Create_WithInvalidCategoryOrAmount_ReturnsBadRequest(string category, string amount)
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsync("/api/trip-expenses", Form(category, amount));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownOrder_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsync("/api/trip-expenses",
            Form(TripExpenseCategories.Alimentacao, "35.00", serviceOrderId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutOrdersWritePermission_IsForbidden()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Warehouse);

        var response = await client.PostAsync("/api/trip-expenses", Form(TripExpenseCategories.Pernoite, "120"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesExpense()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await (await client.PostAsync("/api/trip-expenses",
            Form(TripExpenseCategories.ComissaoMotorista, "80"))).Content.ReadFromJsonAsync<ExpenseResponse>();

        var delete = await client.DeleteAsync($"/api/trip-expenses/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var again = await client.DeleteAsync($"/api/trip-expenses/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    private record ExpenseResponse(
        Guid Id,
        string Category,
        decimal Amount,
        string Description,
        string? PhotoUrl,
        Guid? ServiceOrderId);
}
