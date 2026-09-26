using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;
using RenovoWorkshop.Infrastructure.Services;

namespace RenovoWorkshop.Tests;

// Exclusão de foto enviada por engano: remove o registro, desvincula a foto do painel
// (KM do caminhão) e só apaga no Cloudinary arquivos da pasta do sistema.
public class PhotoDeletionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PhotoDeletionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid orderId, Guid kmPhotoId, Guid otherPhotoId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente Foto" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = $"FT{Guid.NewGuid():N}"[..7], CustomerId = customer.Id };
        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            Number = $"OS-{Guid.NewGuid():N}"[..12],
            ServiceType = ServiceOrderTypes.Guincho,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
        };
        var kmPhoto = new ServiceOrderPhoto { Id = Guid.NewGuid(), ServiceOrderId = order.Id, Url = "https://example.com/painel.jpg" };
        var otherPhoto = new ServiceOrderPhoto { Id = Guid.NewGuid(), ServiceOrderId = order.Id, Url = "https://example.com/lateral.jpg" };
        context.AddRange(customer, vehicle, order, kmPhoto, otherPhoto);
        context.TowServiceDetails.Add(new TowServiceDetails
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = order.Id,
            TruckStartKm = 1000,
            TruckStartKmPhotoUrl = kmPhoto.Url,
        });
        await context.SaveChangesAsync();
        return (order.Id, kmPhoto.Id, otherPhoto.Id);
    }

    [Fact]
    public async Task DeleteKmPhoto_RemovesPhotoAndUnlinksFromTowDetails()
    {
        var (orderId, kmPhotoId, otherPhotoId) = await SeedAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.DeleteAsync($"/api/service-orders/{orderId}/photos/{kmPhotoId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
        Assert.False(await context.ServiceOrderPhotos.AnyAsync(p => p.Id == kmPhotoId));
        Assert.True(await context.ServiceOrderPhotos.AnyAsync(p => p.Id == otherPhotoId));
        var tow = await context.TowServiceDetails.SingleAsync(t => t.ServiceOrderId == orderId);
        Assert.Null(tow.TruckStartKmPhotoUrl);
        Assert.Equal(1000, tow.TruckStartKm);
    }

    [Fact]
    public async Task DeletePhoto_FromAnotherOrder_ReturnsNotFound()
    {
        var (_, kmPhotoId, _) = await SeedAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Administrator);

        var response = await client.DeleteAsync($"/api/service-orders/{Guid.NewGuid()}/photos/{kmPhotoId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePhoto_WithoutOrdersWritePermission_IsForbidden()
    {
        var (orderId, _, otherPhotoId) = await SeedAsync();
        var client = _factory.CreateAuthorizedClient(UserRoles.Warehouse);

        var response = await client.DeleteAsync($"/api/service-orders/{orderId}/photos/{otherPhotoId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/m3acmbwn/image/upload/v1790274780/renovo-workshop/service-orders/rmoiivnwteczgupmiw7h.jpg",
        "renovo-workshop/service-orders/rmoiivnwteczgupmiw7h")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/renovo-workshop/service-orders/abc.png",
        "renovo-workshop/service-orders/abc")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1/outra-pasta/abc.png", null)]
    [InlineData("https://example.com/foto.jpg", null)]
    [InlineData("", null)]
    public void ExtractPublicId_OnlyForSystemFolder(string url, string? expected)
    {
        Assert.Equal(expected, CloudinaryPhotoStorageService.ExtractPublicId(url));
    }
}
