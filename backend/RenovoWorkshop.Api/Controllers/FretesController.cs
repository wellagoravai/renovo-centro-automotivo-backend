using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Application.Exceptions;
using RenovoWorkshop.Application.Interfaces;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageOrders")]
public class FretesController : ControllerBase
{
    private readonly IFreightQuoteService _freightQuoteService;
    private readonly ILogger<FretesController> _logger;

    public FretesController(IFreightQuoteService freightQuoteService, ILogger<FretesController> logger)
    {
        _freightQuoteService = freightQuoteService;
        _logger = logger;
    }

    // Cotação sob demanda (não persiste nada) — o operador confirma os valores
    // e salva junto com o restante dos "Dados do Guincho" pelo endpoint normal
    // de ServiceOrders.
    [HttpPost("cotacao")]
    public async Task<IActionResult> Cotar([FromBody] FreightQuoteRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _freightQuoteService.CotarAsync(new FreightQuoteRequest
            {
                OrigemCidade = dto.OrigemCidade,
                OrigemUf = dto.OrigemUf,
                DestinoCidade = dto.DestinoCidade,
                DestinoUf = dto.DestinoUf,
                PrecoPorKm = dto.PrecoPorKm,
                NumeroEixos = dto.NumeroEixos
            }, cancellationToken);

            return Ok(resultado);
        }
        catch (EnderecoOuPrecoInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RotaNaoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (GoogleRoutesCredenciaisInvalidasException ex)
        {
            _logger.LogError(ex, "Credenciais da Google Routes API inválidas");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Serviço de cotação de frete mal configurado. Contate o suporte." });
        }
        catch (GoogleRoutesIndisponivelException ex)
        {
            _logger.LogWarning(ex, "Google Routes API indisponível ao cotar frete de {Origem}/{OrigemUf} para {Destino}/{DestinoUf}",
                dto.OrigemCidade, dto.OrigemUf, dto.DestinoCidade, dto.DestinoUf);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
