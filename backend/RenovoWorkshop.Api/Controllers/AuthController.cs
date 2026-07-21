using Microsoft.AspNetCore.Mvc;
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
    private readonly RenovoWorkshopDbContext _context;
    private readonly IAuthService _authService;

    public AuthController(RenovoWorkshopDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsActive);
        if (user is null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciais inválidas." });

        var token = _authService.GenerateJwtToken(user.UserName, user.Role);
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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (_context.Users.Any(u => u.UserName == request.Username))
            return Conflict(new { message = "Usuário já existe." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            PasswordHash = _authService.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Login), new { username = user.UserName }, user);
    }

    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Email, string FullName, string Password, string Role);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // Get user from JWT token
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
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
