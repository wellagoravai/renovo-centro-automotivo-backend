using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;
using RenovoWorkshop.Infrastructure.Services;
using RenovoWorkshop.Tests.TestDoubles;

namespace RenovoWorkshop.Tests;

public class WhatsAppReplyProcessorTests
{
    private static RenovoWorkshopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RenovoWorkshopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RenovoWorkshopDbContext(options);
    }

    private static Customer SeedCustomer(RenovoWorkshopDbContext context, string whatsApp = "5511999998888")
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Teste", WhatsApp = whatsApp };
        context.Customers.Add(customer);
        context.SaveChanges();
        return customer;
    }

    private static ServiceOrder SeedOrder(RenovoWorkshopDbContext context, Customer customer, string number, string status = "Aguardando aprovação")
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = number, CustomerId = customer.Id };
        var order = new ServiceOrder { Id = Guid.NewGuid(), Number = number, Status = status, CustomerId = customer.Id, VehicleId = vehicle.Id };
        context.Vehicles.Add(vehicle);
        context.ServiceOrders.Add(order);
        context.SaveChanges();
        return order;
    }

    private static WhatsAppReplyProcessor CreateProcessor(RenovoWorkshopDbContext context, out FakeWhatsAppService whatsApp)
    {
        whatsApp = new FakeWhatsAppService();
        var statusService = new ServiceOrderStatusService(context, whatsApp, new FakeNotificationService());
        return new WhatsAppReplyProcessor(context, statusService, whatsApp);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldReturnNoMatch_WhenPhoneIsUnknown()
    {
        using var context = CreateContext();
        var processor = CreateProcessor(context, out _);

        var result = await processor.ProcessInboundMessageAsync("5511900000000@s.whatsapp.net", "sim", "msg-1");

        Assert.Equal("NoMatch", result.Outcome);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldApprove_WhenSingleOrderPendingAndRepliesSim()
    {
        using var context = CreateContext();
        var customer = SeedCustomer(context);
        var order = SeedOrder(context, customer, "OS-0001");
        var processor = CreateProcessor(context, out _);

        var result = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Sim", "msg-1");

        Assert.Equal("Approved", result.Outcome);
        var reloaded = await context.ServiceOrders.FindAsync(order.Id);
        Assert.Equal("Aprovado", reloaded!.Status);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldCancel_WhenSingleOrderPendingAndRepliesNao()
    {
        using var context = CreateContext();
        var customer = SeedCustomer(context);
        var order = SeedOrder(context, customer, "OS-0002");
        var processor = CreateProcessor(context, out _);

        var result = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Não", "msg-1");

        Assert.Equal("Rejected", result.Outcome);
        var reloaded = await context.ServiceOrders.FindAsync(order.Id);
        Assert.Equal("Cancelado", reloaded!.Status);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldAskToSpecifyOrder_WhenMultiplePendingAndNoNumberGiven()
    {
        using var context = CreateContext();
        var customer = SeedCustomer(context);
        SeedOrder(context, customer, "OS-0003");
        SeedOrder(context, customer, "OS-0004");
        var processor = CreateProcessor(context, out var whatsApp);

        var result = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Sim", "msg-1");

        Assert.Equal("AmbiguousCustomer", result.Outcome);
        Assert.Single(whatsApp.RawMessagesSent);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldDisambiguateByOrderNumber_WhenMultiplePending()
    {
        using var context = CreateContext();
        var customer = SeedCustomer(context);
        SeedOrder(context, customer, "OS-0005");
        var target = SeedOrder(context, customer, "OS-0006");
        var processor = CreateProcessor(context, out _);

        var result = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Sim OS-0006", "msg-1");

        Assert.Equal("Approved", result.Outcome);
        Assert.Equal(target.Id, result.ServiceOrderId);
    }

    [Fact]
    public async Task ProcessInboundMessageAsync_ShouldIgnoreDuplicateProviderMessageId()
    {
        using var context = CreateContext();
        var customer = SeedCustomer(context);
        SeedOrder(context, customer, "OS-0007");
        var processor = CreateProcessor(context, out _);

        var first = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Sim", "msg-dup");
        var second = await processor.ProcessInboundMessageAsync("5511999998888@s.whatsapp.net", "Sim", "msg-dup");

        Assert.Equal("Approved", first.Outcome);
        Assert.Equal("DuplicateIgnored", second.Outcome);
    }
}
