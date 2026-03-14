using ClinicFlow.Common;
using ClinicFlow.Constants;
using ClinicFlow.Data.Seed;
using ClinicFlow.DTOs.Auth;
using ClinicFlow.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClinicFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }


[HttpPost("register")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public async Task<IActionResult> Register(RegisterDto dto)
{
    var normalizedRole = IdentitySeeder.Roles
        .FirstOrDefault(r => r.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));

    if (normalizedRole is null)
    {
        return BadRequest(ApiResponse<object>.FailResponse("Invalid role."));
    }

    if (!await _roleManager.RoleExistsAsync(normalizedRole))
    {
        return BadRequest(ApiResponse<object>.FailResponse("Role does not exist in the system."));
    }

    var existingUser = await _userManager.FindByEmailAsync(dto.Email);
    if (existingUser is not null)
    {
        return BadRequest(ApiResponse<object>.FailResponse("Email is already registered."));
    }

    var user = new ApplicationUser
    {
        FullName = dto.FullName,
        Email = dto.Email,
        UserName = dto.Email
    };

    var result = await _userManager.CreateAsync(user, dto.Password);
    if (!result.Succeeded)
    {
        var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
        return BadRequest(ApiResponse<object>.FailResponse(errors));
    }

    var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
    if (!roleResult.Succeeded)
    {
        await _userManager.DeleteAsync(user);

        var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
        return BadRequest(ApiResponse<object>.FailResponse(errors));
    }

    return Ok(ApiResponse<object>.SuccessResponse(
        new { user.Email, user.FullName, Role = normalizedRole },
        "User registered successfully."));
}

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return Unauthorized(ApiResponse<string>.FailResponse("Invalid email or password."));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!isPasswordValid)
        {
            return Unauthorized(ApiResponse<string>.FailResponse("Invalid email or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            token,
            email = user.Email,
            fullName = user.FullName,
            roles
        }, "Login successful."));
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}