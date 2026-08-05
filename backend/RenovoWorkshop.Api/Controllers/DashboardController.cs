using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;

    public DashboardController(RenovoWorkshopDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        // Guincho tem status e KPIs próprios (ver GetTowDashboard) — este dashboard
        // fica só com o que sempre foi: os atendimentos de oficina.
        var orders = await _context.ServiceOrders.Where(o => o.ServiceType == ServiceOrderTypes.Oficina).ToListAsync();
        var inventory = await _context.InventoryItems.ToListAsync();
        var today = DateTime.UtcNow.Date;

        var delayedOrders = orders.Where(o => o.EstimatedDate.HasValue && o.EstimatedDate < today && o.Status != "Entregue" && o.Status != "Cancelado").ToList();
        var startedToday = orders.Count(o => o.EntryDate.Date == today);
        var completedToday = orders.Count(o => o.FinalDate.HasValue && o.FinalDate.Value.Date == today);
        var lowStock = inventory.Where(i => i.Quantity <= i.MinimumQuantity).ToList();
        var budgets = orders.Count(o => o.Status == "Aguardando aprovação");
        var averageStay = orders.Any()
            ? orders.Where(o => o.FinalDate.HasValue).Average(o => (o.FinalDate!.Value - o.EntryDate).TotalDays)
            : 0;

        var topServices = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.Services))
            .SelectMany(o => o.Services.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Service = g.Key, Count = g.Count() })
            .ToList();

        var monthlyOrders = orders
            .GroupBy(o => new { o.EntryDate.Year, o.EntryDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new { Month = $"{g.Key.Year}-{g.Key.Month:00}", Count = g.Count() })
            .ToList();

        var revenue = orders.Where(o => o.FinalDate.HasValue).Sum(o => o.Value);

        return Ok(new
        {
            vehiclesInDiagnostic = orders.Count(o => o.Status == "Em diagnóstico"),
            vehiclesAwaitingApproval = orders.Count(o => o.Status == "Aguardando aprovação"),
            vehiclesAwaitingParts = orders.Count(o => o.Status == "Aguardando peças"),
            vehiclesInMaintenance = orders.Count(o => o.Status == "Em manutenção"),
            vehiclesInTests = orders.Count(o => o.Status == "Testes"),
            vehiclesInWash = orders.Count(o => o.Status == "Lavagem"),
            vehiclesReadyForDelivery = orders.Count(o => o.Status == "Pronto para retirada"),
            vehiclesFinished = orders.Count(o => o.Status == "Entregue"),
            delayedVehicles = delayedOrders.Count,
            servicesStartedToday = startedToday,
            servicesCompletedToday = completedToday,
            partsAwaitingPurchase = lowStock.Count,
            budgetsAwaitingApproval = budgets,
            averageStayDays = Math.Round(averageStay, 2),
            topServices,
            monthlyOrders,
            revenue,
            lowStockItems = lowStock.Select(i => new { i.Code, i.Description, i.Quantity, i.MinimumQuantity })
        });
    }

    [HttpGet("realtime")]
    public async Task<IActionResult> GetRealtimeSnapshot()
    {
        var orders = await _context.ServiceOrders.Where(o => o.ServiceType == ServiceOrderTypes.Oficina).ToListAsync();
        return Ok(new
        {
            updatedAt = DateTime.UtcNow,
            openOrders = orders.Count(o => o.Status != "Entregue" && o.Status != "Cancelado"),
            finishedOrders = orders.Count(o => o.Status == "Entregue"),
            pendingBudgets = orders.Count(o => o.Status == "Aguardando aprovação")
        });
    }

    // Dashboard separado pra acompanhar veículos em transporte pelo guincho — os
    // status de guincho (ver ServiceOrderStatuses.Guincho) não têm nada a ver com
    // os buckets de oficina do GetDashboard() acima.
    [HttpGet("tow")]
    public async Task<IActionResult> GetTowDashboard()
    {
        var orders = await _context.ServiceOrders.Where(o => o.ServiceType == ServiceOrderTypes.Guincho).ToListAsync();
        var today = DateTime.UtcNow.Date;

        return Ok(new
        {
            callsReceived = orders.Count(o => o.Status == "Chamado recebido"),
            enRouteToPickup = orders.Count(o => o.Status == "A caminho do local"),
            vehicleLoaded = orders.Count(o => o.Status == "Veículo carregado"),
            inTransport = orders.Count(o => o.Status == "Em transporte"),
            deliveredToday = orders.Count(o => o.Status == "Entregue" && o.FinalDate.HasValue && o.FinalDate.Value.Date == today),
            cancelled = orders.Count(o => o.Status == "Cancelado"),
            activeCalls = orders.Count(o => o.Status != "Entregue" && o.Status != "Cancelado"),
            callsStartedToday = orders.Count(o => o.EntryDate.Date == today)
        });
    }
}
