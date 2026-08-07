using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Depois de MaxFailedAttempts tentativas erradas seguidas pro MESMO usuário, a conta
    // fica bloqueada por LockoutDuration — isso é por conta (independe de IP), e existe
    // junto com o rate limiter por IP em Program.cs (que trava a velocidade das tentativas
    // de qualquer origem); um cobre o outro contra força bruta lenta e distribuída.
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly RenovoWorkshopDbContext _context;
    private readonly IAuthService _authService;

    public AuthController(RenovoWorkshopDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsActive);

        if (user is not null && user.LockedOutUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            var minutesLeft = Math.Max(1, (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes));
            return StatusCode(StatusCodes.Status423Locked, new
            {
                message = $"Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em {minutesLeft} minuto(s)."
            });
        }

        if (user is null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockedOutUntil = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                }
                await _context.SaveChangesAsync();
            }

            // Mesma mensagem genérica tanto pra usuário inexistente quanto pra senha errada —
            // não dá pra um invasor descobrir por tentativa se um username existe ou não.
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        if (user.FailedLoginAttempts > 0 || user.LockedOutUntil.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockedOutUntil = null;
            await _context.SaveChangesAsync();
        }

        var token = _authService.GenerateJwtToken(user.Id, user.UserName, user.Role);
        var permissions = UserPermissions.ForRole(user.Role);
        return Ok(new { 
            token, 
            user = new { 
                user.Id, 
                user.UserName, 
                user.FullName, 
                user.Role,
                Permissions = string.Join(",", permissions)
            } 
        });
    }

    // Criação de usuários é uma operação administrativa: exige token válido e a
    // policy CanManageUsers, assim como RenovoWorkshop.Api.Controllers.UsersController.Create.
    // Sem isso, qualquer pessoa sem login poderia se cadastrar com Role="Administrador".
    [Authorize(Policy = "CanManageUsers")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!UserRoles.All.Contains(request.Role))
            return BadRequest(new { message = "Papel (role) inválido." });

        if (await _context.Users.AnyAsync(u => u.UserName == request.Username))
            return Conflict(new { message = "Usuário já existe." });

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email já cadastrado." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            Permissions = string.Join(",", UserPermissions.ForRole(request.Role)),
            PasswordHash = _authService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.Role,
            user.Permissions,
            user.IsActive,
            user.CreatedAt
        };
        return CreatedAtAction(nameof(Login), new { username = user.UserName }, userDto);
    }

    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Email, string FullName, string Password, string Role);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // Get user from JWT token
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Usuário não autenticado." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user is null)
            return NotFound(new { message = "Usuário não encontrado." });

        // Validate current password
        if (!_authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Senha atual incorreta." });

        // Validate new password
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "A nova senha e a confirmação não coincidem." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "A nova senha deve ter pelo menos 6 caracteres." });

        // Update password
        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Senha alterada com sucesso!" });
    }
}
