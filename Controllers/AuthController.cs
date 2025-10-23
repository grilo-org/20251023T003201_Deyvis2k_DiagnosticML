using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CSProject.Data;
using CSProject.Models;
using CSProject.Core.DTO;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly CSProjectContext _dbContext;

    public AuthController(IConfiguration config, CSProjectContext dbContext)
    {
        _config = config;
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        if (_dbContext.Users == null)
        {
            return StatusCode(500, "Erro interno: tabela de usuários não disponível");
        }
        
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == loginDto.Email);

        if (user == null || !user.VerifyPassword(loginDto.Password))
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        // Create claims for the user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // Create claims identity
        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Create authentication properties
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = loginDto.RememberMe, // Use user preference for persistence
            ExpiresUtc = DateTime.UtcNow.AddHours(2) // Same expiration as the previous JWT token
        };

        // Sign in the user with cookie authentication
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok(new { Success = true, Message = "Login realizado com sucesso!" });
    }
    // Added a logout method for completeness
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { Success = true, Message = "Logout realizado com sucesso!" });
    }
}

