using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Domain.Constants;
using RenovoWorkshop.Domain.Entities;
using RenovoWorkshop.Infrastructure.Persistence;

namespace RenovoWorkshop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RenovoWorkshopDbContext _context;
    private readonly IMapper _mapper;

    public UsersController(RenovoWorkshopDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? role = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.UserName.Contains(term) || u.Email.Contains(term) || u.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userDtos = _mapper.Map<List<UserDto>>(users);
        return Ok(userDtos);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto createUserDto)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == createUserDto.UserName))
            return Conflict(new { message = "Nome de usuário já existe." });

        if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            return Conflict(new { message = "Email já cadastrado." });

        var user = _mapper.Map<ApplicationUser>(createUserDto);
        user.Id = Guid.NewGuid();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
        user.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(user.Permissions))
        {
            user.Permissions = string.Join(",", UserPermissions.ForRole(user.Role));
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, userDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (user.UserName != updateUserDto.UserName && await _context.Users.AnyAsync(u => u.UserName == updateUserDto.UserName))
            return Conflict(new { message = "Nome de usuário já existe." });

        if (user.Email != updateUserDto.Email && await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email))
            return Conflict(new { message = "Email já cadastrado." });

        _mapper.Map(updateUserDto, user);

        if (string.IsNullOrWhiteSpace(user.Permissions))
        {
            user.Permissions = string.Join(",", UserPermissions.ForRole(user.Role));
        }

        await _context.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = UserRoles.All.Select(r => new { Value = r, Label = r }).ToList();
        return Ok(roles);
    }
}