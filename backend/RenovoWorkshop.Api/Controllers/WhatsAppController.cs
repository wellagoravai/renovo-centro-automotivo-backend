using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageOrders")]
public class WhatsAppController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IWhatsAppReplyProcessor _replyProcessor;
    private readonly IConfiguration _configuration;

    public WhatsAppController(RenovoWorkshopDbContext context, IWhatsAppReplyProcessor replyProcessor, IConfiguration configuration)
    {
        _context = context;
        _replyProcessor = replyProcessor;
        _configuration = configuration;
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

    // A Evolution API não autentica como um usuário do sistema, então este endpoint não
    // pode usar o [Authorize] de classe — a segurança aqui é o token compartilhado abaixo.
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload, [FromHeader(Name = "X-Webhook-Token")] string? headerToken, [FromQuery] string? token)
    {
        var expectedToken = _configuration["WhatsApp:WebhookToken"];
        var providedToken = headerToken ?? token;
        if (string.IsNullOrEmpty(expectedToken) || !string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var eventType = payload.TryGetProperty("event", out var eventProp) ? eventProp.GetString() : null;
        if (!string.Equals(eventType, "messages.upsert", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { received = true });
        }

        if (!payload.TryGetProperty("data", out var data))
        {
            return Ok(new { received = true });
        }

        var fromMe = data.TryGetProperty("key", out var key) && key.TryGetProperty("fromMe", out var fromMeProp) && fromMeProp.ValueKind == JsonValueKind.True;
        if (fromMe)
        {
            return Ok(new { received = true });
        }

        var remoteJid = key.ValueKind == JsonValueKind.Object && key.TryGetProperty("remoteJid", out var remoteJidProp) ? remoteJidProp.GetString() : null;
        var messageId = key.ValueKind == JsonValueKind.Object && key.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        string? text = null;
        if (data.TryGetProperty("message", out var messageProp))
        {
            if (messageProp.TryGetProperty("conversation", out var conversationProp))
            {
                text = conversationProp.GetString();
            }
            else if (messageProp.TryGetProperty("extendedTextMessage", out var extendedProp) && extendedProp.TryGetProperty("text", out var extendedTextProp))
            {
                text = extendedTextProp.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(remoteJid) || string.IsNullOrWhiteSpace(text))
        {
            return Ok(new { received = true });
        }

        await _replyProcessor.ProcessInboundMessageAsync(remoteJid, text, messageId, HttpContext.RequestAborted);
        return Ok(new { received = true });
    }
}
