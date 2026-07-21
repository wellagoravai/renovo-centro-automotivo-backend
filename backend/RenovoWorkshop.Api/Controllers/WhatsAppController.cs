using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageOrders")]
public class WhatsAppController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;

    public WhatsAppController(RenovoWorkshopDbContext context)
    {
        _context = context;
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] Guid? orderId = null)
    {
        var query = _context.WhatsAppMessageLogs.AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(log => log.ServiceOrderId == orderId.Value);
        }

        var logs = await query.OrderByDescending(log => log.SentAt).ToListAsync();
        return Ok(logs);
    }
}
