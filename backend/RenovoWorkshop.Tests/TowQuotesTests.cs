using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Tests;

// Histórico de orçamentos de guincho: salvar/reabrir o formulário, guardar o PDF,
// aprovar ligando à OS aberta e as regras de edição depois da decisão.
public class TowQuotesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TowQuotesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record QuoteResponse(
        Guid Id,
        string Number,
        string Status,
        string CustomerName,
        string VehiclePlate,
        decimal Total,
        bool HasPdf,
        Guid? ServiceOrderId,
        string? ServiceOrderNumber,
        string FormData);

    private static object Body(string customer = "Cliente Orçamento", decimal total = 530m, string formData = "{\"notes\":\"teste\"}") => new
    {
        customerName = customer,
        customerPhone = "(18) 99999-0000",
        vehiclePlate = "ABC1D23",
        vehicleDescription = "Toyota Hilux",
        routeSummary = "Andradina/SP → Araçatuba/SP",
        totalKm = 140m,
        total,
        formData,
    };

    private async Task<Guid> SeedTowOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente OS" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"TQ{Guid.NewGuid():N}"[..7], CustomerId = customer.Id };
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

    private static async Task<QuoteResponse> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/tow-quotes", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<QuoteResponse>())!;
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsPendingQuoteWithFormData()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var created = await CreateAsync(client, Body(formData: "{\"customer\":{\"name\":\"João\"}}"));

        Assert.StartsWith("ORC-", created.Number);
        Assert.Equal(TowQuoteStatuses.Pendente, created.Status);
        Assert.Equal(530m, created.Total);

        var fetched = await client.GetFromJsonAsync<QuoteResponse>($"/api/tow-quotes/{created.Id}");
        Assert.Equal("{\"customer\":{\"name\":\"João\"}}", fetched!.FormData);
    }

    [Fact]
    public async Task Create_WithInvalidFormData_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.PostAsJsonAsync("/api/tow-quotes", Body(formData: "não é json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatusAndSearchIgnoringAccents()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var unique = $"Açúcar {Guid.NewGuid():N}"[..14];
        var created = await CreateAsync(client, Body(customer: unique));

        var bySearch = await client.GetFromJsonAsync<List<QuoteResponse>>($"/api/tow-quotes?search={Uri.EscapeDataString("acucar" + unique[6..])}");
        Assert.Contains(bySearch!, q => q.Id == created.Id);

        var approvedOnly = await client.GetFromJsonAsync<List<QuoteResponse>>("/api/tow-quotes?status=Aprovado");
        Assert.DoesNotContain(approvedOnly!, q => q.Id == created.Id);
    }

    [Fact]
    public async Task UploadPdf_ThenDownload_ReturnsSameBytes()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());
        var pdfBytes = "%PDF-1.4 conteudo de teste"u8.ToArray();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "orcamento.pdf");
        var upload = await client.PutAsync($"/api/tow-quotes/{created.Id}/pdf", form);
        Assert.Equal(HttpStatusCode.NoContent, upload.StatusCode);

        var download = await client.GetAsync($"/api/tow-quotes/{created.Id}/pdf");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(pdfBytes, await download.Content.ReadAsByteArrayAsync());

        var list = await client.GetFromJsonAsync<List<QuoteResponse>>("/api/tow-quotes");
        Assert.True(list!.Single(q => q.Id == created.Id).HasPdf);
    }

    [Fact]
    public async Task UploadPdf_RejectsNonPdfFile()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());

        var form = new MultipartFormDataContent { { new ByteArrayContent("não é pdf"u8.ToArray()), "file", "x.pdf" } };
        var upload = await client.PutAsync($"/api/tow-quotes/{created.Id}/pdf", form);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Approve_LinksOrder_AndFreezesQuote()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());
        var orderId = await SeedTowOrderAsync();

        var approve = await client.PostAsJsonAsync($"/api/tow-quotes/{created.Id}/approve", new { serviceOrderId = orderId });
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var approved = (await approve.Content.ReadFromJsonAsync<QuoteResponse>())!;
        Assert.Equal(TowQuoteStatuses.Aprovado, approved.Status);
        Assert.Equal(orderId, approved.ServiceOrderId);
        Assert.NotNull(approved.ServiceOrderNumber);

        // Retry da mesma aprovação é idempotente.
        var retry = await client.PostAsJsonAsync($"/api/tow-quotes/{created.Id}/approve", new { serviceOrderId = orderId });
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/tow-quotes/{created.Id}", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/tow-quotes/{created.Id}/reject", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/tow-quotes/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task EditingRejectedQuote_ReturnsItToPending()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);
        var created = await CreateAsync(client, Body());

        var reject = await client.PostAsync($"/api/tow-quotes/{created.Id}/reject", null);
        Assert.Equal(TowQuoteStatuses.Recusado, (await reject.Content.ReadFromJsonAsync<QuoteResponse>())!.Status);

        var update = await client.PutAsJsonAsync($"/api/tow-quotes/{created.Id}", Body(total: 480m));
        var updated = (await update.Content.ReadFromJsonAsync<QuoteResponse>())!;
        Assert.Equal(TowQuoteStatuses.Pendente, updated.Status);
        Assert.Equal(480m, updated.Total);
    }

    [Fact]
    public async Task Create_WithoutOrdersWritePermission_IsForbidden()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Warehouse);

        var response = await client.PostAsJsonAsync("/api/tow-quotes", Body());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_AsClient_IsForbidden()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Client);

        var response = await client.GetAsync("/api/tow-quotes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
