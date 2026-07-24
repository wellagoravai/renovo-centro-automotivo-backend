using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class ServiceOrderStatusServiceTests
{
    private static RenovoWorkshopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RenovoWorkshopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RenovoWorkshopDbContext(options);
    }

    private static ServiceOrder SeedOrder(RenovoWorkshopDbContext context, string status)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Teste", WhatsApp = "5511999998888" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = "ABC1234", CustomerId = customer.Id };
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = "OS-TESTE-0001",
            Status = status,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id
        };

        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        context.ServiceOrders.Add(order);
        context.SaveChanges();
        return order;
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldAddHistoryAndNotify_WhenStatusChanges()
    {
        using var context = CreateContext();
        var order = SeedOrder(context, "Aguardando aprovação");
        var whatsApp = new FakeWhatsAppService();
        var service = new ServiceOrderStatusService(context, whatsApp);

        var result = await service.ChangeStatusAsync(order.Id, "Aprovado", null, "Equipe");

        Assert.True(result.Success);
        Assert.Equal("Aprovado", result.Order!.Status);
        Assert.Equal(1, whatsApp.StatusMessagesSent);
        Assert.Single(context.ServiceOrderHistories.Where(h => h.ServiceOrderId == order.Id));
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldBeNoOp_WhenStatusIsUnchanged()
    {
        using var context = CreateContext();
        var order = SeedOrder(context, "Aprovado");
        var whatsApp = new FakeWhatsAppService();
        var service = new ServiceOrderStatusService(context, whatsApp);

        var result = await service.ChangeStatusAsync(order.Id, "Aprovado", null, "Equipe");

        Assert.True(result.Success);
        Assert.Equal(0, whatsApp.StatusMessagesSent);
        Assert.Empty(context.ServiceOrderHistories.Where(h => h.ServiceOrderId == order.Id));
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        using var context = CreateContext();
        var whatsApp = new FakeWhatsAppService();
        var service = new ServiceOrderStatusService(context, whatsApp);

        var result = await service.ChangeStatusAsync(Guid.NewGuid(), "Aprovado", null, "Equipe");

        Assert.False(result.Success);
        Assert.Null(result.Order);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldDeductStockOnlyOnce_WhenDeliveredTwice()
    {
        using var context = CreateContext();
        var order = SeedOrder(context, "Pronto para retirada");
        var inventoryItem = new InventoryItem { Id = Guid.NewGuid(), Code = "P1", Description = "Peça", Quantity = 10 };
        context.InventoryItems.Add(inventoryItem);
        context.ServiceOrderItems.Add(new ServiceOrderItem { Id = Guid.NewGuid(), ServiceOrderId = order.Id, InventoryItemId = inventoryItem.Id, Quantity = 3, UnitValue = 10 });
        context.SaveChanges();

        var whatsApp = new FakeWhatsAppService();
        var service = new ServiceOrderStatusService(context, whatsApp);

        await service.ChangeStatusAsync(order.Id, "Entregue", null, "Equipe");
        Assert.Equal(7, (await context.InventoryItems.FindAsync(inventoryItem.Id))!.Quantity);

        // Segunda chamada com o mesmo status é a guarda de idempotência (curto-circuita antes
        // de checar StockDeducted), então não deve debitar de novo.
        await service.ChangeStatusAsync(order.Id, "Entregue", null, "Equipe");
        Assert.Equal(7, (await context.InventoryItems.FindAsync(inventoryItem.Id))!.Quantity);
    }
}
