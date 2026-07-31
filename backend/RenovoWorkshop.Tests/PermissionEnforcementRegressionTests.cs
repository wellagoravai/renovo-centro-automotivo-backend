using System.Net;
using System.Net.Http.Json;
using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Tests;

// Garante que cada papel só consegue escrever nos módulos que o manual do
// usuário promete para ele (ver UserPermissions.ForRole). Cobre o pedido de
// regressão de "permissões" ao lado dos fluxos de OS e estoque.
public class PermissionEnforcementRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PermissionEnforcementRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reception_CannotManageInventory()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Reception);

        var response = await client.PostAsJsonAsync("/api/inventory", new
        {
            code = $"P-{Guid.NewGuid():N}",
            description = "Peça de teste",
            quantity = 10,
            minimumQuantity = 2
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Warehouse_CanManageInventory()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Warehouse);

        var response = await client.PostAsJsonAsync("/api/inventory", new
        {
            code = $"P-{Guid.NewGuid():N}",
            description = "Peça de teste",
            quantity = 10,
            minimumQuantity = 2
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Warehouse_CannotManageCustomers()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Warehouse);

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            name = "Cliente de Teste",
            document = "12345678901",
            phone = "(11) 90000-0000",
            whatsApp = "(11) 90000-0000",
            email = "cliente@teste.com",
            address = "Rua Teste, 1",
            notes = ""
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reception_CanManageCustomers()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Reception);

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            name = $"Cliente {Guid.NewGuid():N}",
            document = Guid.NewGuid().ToString("N")[..11],
            phone = "(11) 90000-0000",
            whatsApp = "(11) 90000-0000",
            email = $"{Guid.NewGuid():N}@teste.com",
            address = "Rua Teste, 1",
            notes = ""
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Mechanic_CannotManageUsers()
    {
        var client = _factory.CreateAuthorizedClient(UserRoles.Mechanic);

        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Mechanic_CanReadButNotWriteVehicles()
    {
        var readClient = _factory.CreateAuthorizedClient(UserRoles.Mechanic);
        var readResponse = await readClient.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var writeClient = _factory.CreateAuthorizedClient(UserRoles.Mechanic);
        var writeResponse = await writeClient.PostAsJsonAsync("/api/vehicles", new
        {
            plate = $"TST{Guid.NewGuid():N}"[..7].ToUpperInvariant(),
            brand = "Marca",
            model = "Modelo",
            year = 2024,
            customerId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }
}
