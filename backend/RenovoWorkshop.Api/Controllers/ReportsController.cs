using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;

    public ReportsController(RenovoWorkshopDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Relatório de manutenções concluídas
    /// </summary>
    [HttpGet("completed-maintenance")]
    public async Task<IActionResult> GetCompletedMaintenance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;

            var completedOrders = await _context.ServiceOrders
                .Include(o => o.Customer)
                .Include(o => o.Vehicle)
                .Where(o => o.Status == "Entregue" &&
                           o.FinalDate >= start &&
                           o.FinalDate <= end.AddDays(1).AddSeconds(-1))
                .OrderByDescending(o => o.FinalDate)
                .ToListAsync();

            var report = completedOrders.Select(o => new CompletedMaintenanceReportDto
            {
                Id = o.Id,
                OrderNumber = o.Number,
                CustomerName = o.Customer.Name,
                VehiclePlate = o.Vehicle.Plate,
                VehicleBrand = o.Vehicle.Brand,
                VehicleModel = o.Vehicle.Model,
                Services = o.Services,
                Parts = o.Parts,
                Value = o.Value,
                EntryDate = o.EntryDate,
                FinalDate = o.FinalDate ?? DateTime.UtcNow,
                ResponsibleUser = o.ResponsibleUser,
                DurationHours = o.FinalDate.HasValue ? 
                    (o.FinalDate.Value - o.EntryDate).TotalHours : 0
            }).ToList();

            var summary = new
            {
                TotalCompleted = report.Count,
                TotalValue = report.Sum(r => r.Value),
                AverageValue = report.Count > 0 ? report.Average(r => r.Value) : 0,
                AverageDuration = report.Count > 0 ? report.Average(r => r.DurationHours) : 0
            };

            return Ok(new { summary, data = report });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório", error = ex.Message });
        }
    }

    /// <summary>
    /// Visão geral de serviços
    /// </summary>
    [HttpGet("services-overview")]
    public async Task<IActionResult> GetServicesOverview([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;

            var orders = await _context.ServiceOrders
                .Where(o => o.EntryDate >= start && 
                           o.EntryDate <= end.AddDays(1).AddSeconds(-1))
                .ToListAsync();

            // Separar serviços por vírgula e contar
            var servicesList = orders
                .SelectMany(o => o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .GroupBy(s => s)
                .Select(g => new ServiceStatDto
                {
                    ServiceName = g.Key,
                    Count = g.Count(),
                    TotalValue = orders
                        .Where(o => o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Contains(g.Key))
                        .Sum(o => o.Value)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            var cancelled = orders.Count(o => o.Status == "Cancelado");

            var overview = new
            {
                Period = new { start, end },
                TotalOrders = orders.Count,
                TotalValue = orders.Sum(o => o.Value),
                AverageOrderValue = orders.Count > 0 ? orders.Average(o => o.Value) : 0,
                CancellationRate = orders.Count > 0 ? (double)cancelled / orders.Count : 0,
                Services = servicesList,
                StatusBreakdown = orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList()
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar visão geral", error = ex.Message });
        }
    }

    /// <summary>
    /// Serviços mais efetuados por período (dia, semana, mês)
    /// </summary>
    [HttpGet("top-services")]
    public async Task<IActionResult> GetTopServices([FromQuery] string period = "week", [FromQuery] int top = 10)
    {
        try
        {
            DateTime startDate;
            var endDate = DateTime.UtcNow.Date;

            switch (period.ToLower())
            {
                case "day":
                    startDate = endDate;
                    break;
                case "week":
                    startDate = endDate.AddDays(-7);
                    break;
                case "month":
                    startDate = endDate.AddMonths(-1);
                    break;
                default:
                    startDate = endDate.AddDays(-7);
                    break;
            }

            var orders = await _context.ServiceOrders
                .Where(o => o.EntryDate >= startDate && 
                           o.EntryDate <= endDate.AddDays(1).AddSeconds(-1))
                .ToListAsync();

            var topServices = orders
                .SelectMany(o => o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .GroupBy(s => s)
                .Select(g => new TopServiceDto
                {
                    ServiceName = g.Key,
                    Count = g.Count(),
                    TotalRevenue = orders
                        .Where(o => o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Contains(g.Key))
                        .Sum(o => o.Value / o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries).Length)
                })
                .OrderByDescending(s => s.Count)
                .Take(top)
                .ToList();

            return Ok(new
            {
                period,
                startDate,
                endDate,
                totalOrders = orders.Count,
                services = topServices
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório", error = ex.Message });
        }
    }

    /// <summary>
    /// Relatório de faturamento por período
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;

            var orders = await _context.ServiceOrders
                .Where(o => o.EntryDate >= start && 
                           o.EntryDate <= end.AddDays(1).AddSeconds(-1))
                .ToListAsync();

            var revenueByDay = orders
                .GroupBy(o => o.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.Value)
                })
                .OrderBy(r => r.Date)
                .ToList();

            var totalRevenue = orders.Sum(o => o.Value);
            var averageOrderValue = orders.Count > 0 ? orders.Average(o => o.Value) : 0;

            return Ok(new
            {
                period = new { start, end },
                totalRevenue,
                averageOrderValue,
                totalOrders = orders.Count,
                dailyRevenue = revenueByDay
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório de faturamento", error = ex.Message });
        }
    }

    /// <summary>
    /// Ordens de serviço num período (usado pelo relatório diário/semanal/mensal do front)
    /// </summary>
    [HttpGet("service-orders")]
    public async Task<IActionResult> GetServiceOrdersInPeriod([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var orders = await _context.ServiceOrders
                .Include(o => o.Customer)
                .Include(o => o.Vehicle)
                .Where(o => o.EntryDate >= start && o.EntryDate <= end)
                .OrderByDescending(o => o.EntryDate)
                .Select(o => new
                {
                    o.Id,
                    o.Number,
                    o.Status,
                    o.EntryDate,
                    CustomerName = o.Customer.Name,
                    VehiclePlate = o.Vehicle.Plate,
                    o.Value,
                })
                .ToListAsync();

            return Ok(new
            {
                totalOrders = orders.Count,
                totalValue = orders.Sum(o => o.Value),
                orders,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório de ordens por período", error = ex.Message });
        }
    }

    /// <summary>
    /// Funcionário que mais realizou serviços no período
    /// </summary>
    [HttpGet("top-employees")]
    public async Task<IActionResult> GetTopEmployees([FromQuery] string period = "month")
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = period.ToLower() == "week" ? endDate.AddDays(-7) : endDate.AddMonths(-1);

            var orders = await _context.ServiceOrders
                .Where(o => o.Status == "Entregue" &&
                           o.FinalDate >= startDate &&
                           o.FinalDate <= endDate.AddDays(1).AddSeconds(-1) &&
                           o.ResponsibleUser != "")
                .ToListAsync();

            var employees = orders
                .GroupBy(o => o.ResponsibleUser)
                .Select(g => new
                {
                    Employee = g.Key,
                    CompletedOrders = g.Count(),
                    TotalValue = g.Sum(o => o.Value),
                })
                .OrderByDescending(e => e.CompletedOrders)
                .ToList();

            return Ok(new { period, startDate, endDate, employees });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório de funcionários", error = ex.Message });
        }
    }

    /// <summary>
    /// Estoque atual por categoria e itens abaixo do mínimo
    /// </summary>
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport()
    {
        try
        {
            var items = await _context.InventoryItems.ToListAsync();

            var byCategory = items
                .GroupBy(i => i.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    ItemCount = g.Count(),
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalValue = g.Sum(i => i.Quantity * i.SaleValue),
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            var lowStock = items
                .Where(i => i.Quantity <= i.MinimumQuantity)
                .Select(i => new { i.Id, i.Code, i.Description, i.Category, i.Quantity, i.MinimumQuantity })
                .ToList();

            return Ok(new
            {
                totalItems = items.Count,
                totalValue = items.Sum(i => i.Quantity * i.SaleValue),
                byCategory,
                lowStock,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório de estoque", error = ex.Message });
        }
    }

    /// <summary>
    /// Serviços mais realizados ao longo de um ano, quebrado por mês
    /// </summary>
    [HttpGet("annual-services")]
    public async Task<IActionResult> GetAnnualServices([FromQuery] int? year = null)
    {
        try
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var start = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddYears(1).AddSeconds(-1);

            var orders = await _context.ServiceOrders
                .Where(o => o.EntryDate >= start && o.EntryDate <= end)
                .ToListAsync();

            var monthly = orders
                .GroupBy(o => o.EntryDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    OrderCount = g.Count(),
                    TotalValue = g.Sum(o => o.Value),
                })
                .OrderBy(m => m.Month)
                .ToList();

            var topServicesYear = orders
                .SelectMany(o => o.Services.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .GroupBy(s => s)
                .Select(g => new TopServiceDto { ServiceName = g.Key, Count = g.Count(), TotalRevenue = 0 })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            return Ok(new { year = targetYear, totalOrders = orders.Count, monthly, topServices = topServicesYear });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório anual", error = ex.Message });
        }
    }

    /// <summary>
    /// Consumo de peças/insumos do estoque nas ordens de serviço concluídas
    /// </summary>
    [HttpGet("parts-consumption")]
    public async Task<IActionResult> GetPartsConsumption([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            var end = endDate?.Date ?? DateTime.UtcNow.Date;

            var items = await _context.ServiceOrderItems
                .Include(i => i.InventoryItem)
                .Include(i => i.ServiceOrder)
                .Where(i => i.ServiceOrder.EntryDate >= start && i.ServiceOrder.EntryDate <= end.AddDays(1).AddSeconds(-1))
                .ToListAsync();

            var consumption = items
                .GroupBy(i => new { i.InventoryItemId, i.InventoryItem.Code, i.InventoryItem.Description, i.InventoryItem.Category })
                .Select(g => new
                {
                    g.Key.Code,
                    g.Key.Description,
                    g.Key.Category,
                    QuantityUsed = g.Sum(i => i.Quantity),
                    TotalValue = g.Sum(i => i.Quantity * i.UnitValue),
                })
                .OrderByDescending(c => c.QuantityUsed)
                .ToList();

            return Ok(new { period = new { start, end }, consumption });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao gerar relatório de consumo de peças", error = ex.Message });
        }
    }
}

public class CompletedMaintenanceReportDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string Services { get; set; } = string.Empty;
    public string Parts { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime FinalDate { get; set; }
    public string ResponsibleUser { get; set; } = string.Empty;
    public double DurationHours { get; set; }
}

public class ServiceStatDto
{
    public string ServiceName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class TopServiceDto
{
    public string ServiceName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalRevenue { get; set; }
}