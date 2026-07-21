using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;

    public AccountController(RenovoWorkshopDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (user is null)
            return NotFound();

        return Ok(new { user.Id, user.UserName, user.FullName, user.Role, user.Permissions });
    }
}
