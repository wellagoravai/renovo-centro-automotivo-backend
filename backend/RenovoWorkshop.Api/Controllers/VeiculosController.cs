using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Domain.Validation;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanQueryVehicleData")]
public class VeiculosController : ControllerBase
{
    private readonly IVeiculoConsultaService _veiculoConsultaService;
    private readonly RenovoWorkshopDbContext _context;
    private readonly ILogger<VeiculosController> _logger;

    public VeiculosController(
        IVeiculoConsultaService veiculoConsultaService,
        RenovoWorkshopDbContext context,
        ILogger<VeiculosController> logger)
    {
        _veiculoConsultaService = veiculoConsultaService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("{placa}")]
    public async Task<IActionResult> GetByPlaca(string placa, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var info = await _veiculoConsultaService.ConsultarPorPlacaAsync(placa, cancellationToken);
            await RegistrarAuditoriaAsync(userId, placa, info.FonteDados, sucesso: true, erro: null, cancellationToken);
            await PreencherChassiAsync(info, cancellationToken);
            return Ok(info);
        }
        catch (PlacaInvalidaException ex)
        {
            await RegistrarAuditoriaAsync(userId, placa, "N/A", sucesso: false, erro: ex.Message, cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            await RegistrarAuditoriaAsync(userId, placa, "ApiBrasil", sucesso: false, erro: ex.Message, cancellationToken);
            return NotFound(new { message = ex.Message });
        }
        catch (ApiBrasilLimiteExcedidoException ex)
        {
            await RegistrarAuditoriaAsync(userId, placa, "ApiBrasil", sucesso: false, erro: ex.Message, cancellationToken);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
        catch (ApiBrasilCredenciaisInvalidasException ex)
        {
            _logger.LogError(ex, "Credenciais da APIBrasil inválidas");
            await RegistrarAuditoriaAsync(userId, placa, "ApiBrasil", sucesso: false, erro: ex.Message, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Serviço de consulta veicular mal configurado. Contate o suporte." });
        }
        catch (ApiBrasilIndisponivelException ex)
        {
            _logger.LogWarning(ex, "APIBrasil indisponível ao consultar placa {Placa}", placa);
            await RegistrarAuditoriaAsync(userId, placa, "ApiBrasil", sucesso: false, erro: ex.Message, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    // O chassi que veio da consulta já fica gravado no cadastro do veículo com essa
    // placa (aba Veículos), sem precisar redigitar. A placa é comparada sem traço:
    // o check-in do app grava "ABC-1D23", o painel às vezes "ABC1D23".
    private async Task PreencherChassiAsync(VeiculoInfo info, CancellationToken cancellationToken)
    {
        var chassi = info.Chassi?.Trim().ToUpperInvariant();
        var placa = PlacaValidator.Normalize(info.Placa);
        if (string.IsNullOrWhiteSpace(chassi) || string.IsNullOrWhiteSpace(placa)) return;

        try
        {
            var veiculos = await _context.Vehicles
                .Where(v => v.Plate.ToUpper().Replace("-", "").Replace(" ", "") == placa && v.Chassis != chassi)
                .ToListAsync(cancellationToken);
            if (veiculos.Count == 0) return;

            foreach (var veiculo in veiculos) veiculo.Chassis = chassi;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // A consulta já foi paga e respondida; falhar ao gravar o chassi não a derruba.
            _logger.LogError(ex, "Falha ao gravar o chassi consultado para a placa {Placa}", placa);
        }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }

    // Log de auditoria (LGPD): quem consultou, qual placa, quando e a origem do
    // dado — persistido mesmo quando a consulta falha, para rastrear tentativas.
    private async Task RegistrarAuditoriaAsync(Guid userId, string placa, string fonteDados, bool sucesso, string? erro, CancellationToken cancellationToken)
    {
        try
        {
            _context.VehicleLookupAuditLogs.Add(new VehicleLookupAuditLog
            {
                UserId = userId,
                Placa = (placa ?? string.Empty).Trim().ToUpperInvariant(),
                FonteDados = fonteDados,
                Sucesso = sucesso,
                Erro = erro
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Falha ao gravar auditoria não pode derrubar a resposta da consulta em si.
            _logger.LogError(ex, "Falha ao registrar auditoria de consulta veicular para a placa {Placa}", placa);
        }
    }
}
