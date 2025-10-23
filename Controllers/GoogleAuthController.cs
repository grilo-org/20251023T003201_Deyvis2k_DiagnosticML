using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using CSProject.Data;
using CSProject.Models;
using CSProject.Core.DTO;
using CSProject.Core.Enums;

namespace CSProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthController> _logger;
        private readonly CSProjectContext _databaseContext;

        public GoogleAuthController(IConfiguration configuration, ILogger<GoogleAuthController> logger, CSProjectContext databaseContext)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("signin")]
        public IActionResult SignIn()
        {
            try
            {
                _logger.LogInformation("Initiating Google sign-in process");
                
                var properties = new AuthenticationProperties 
                { 
                    RedirectUri = Url.Action(nameof(HandleCallback)),
                    Items =
                    {
                        { "returnUrl", Url.Action("Index", "Home") }
                    }
                };
                return Challenge(properties, GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google sign-in");
                return StatusCode(500, "An error occurred while initiating the sign-in process");
            }
        }

        [HttpGet("callback")]
        public async Task<IActionResult> HandleCallback()
        {
            try
            {
                _logger.LogInformation("Processing Google authentication callback");
                
                var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!authenticateResult.Succeeded)
                {
                    _logger.LogWarning("External authentication failed");
                    return RedirectToAction("Login", "Account");
                }

                var externalUser = authenticateResult.Principal;

                if (externalUser == null)
                {
                    _logger.LogWarning("External authentication succeeded but no user principal was provided");
                    return RedirectToAction("Login", "Account");
                }

                var userIdClaim = externalUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = externalUser.FindFirst(ClaimTypes.Email)?.Value;
                var nameClaim = externalUser.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim))
                {
                    _logger.LogWarning("Required claims missing from Google authentication");
                    return RedirectToAction("Login", "Account");
                }

                User? user = null;

                try
                {
                    user = _databaseContext.Users?.FirstOrDefault(u => u.Email == emailClaim);
                }
                catch
                {
                    _logger.LogError("Error retrieving user from database");
                    return StatusCode(500, "An error occurred while retrieving user from database");
                }

                if (user == null)
                {
                    var userDTO = new RegisterUserDTO
                    {
                        Name = nameClaim ?? "Google User",
                        Email = emailClaim,
                        Password = "GOOGLE_AUTH" + Guid.NewGuid().ToString(),
                        IsGoogleAuthenticated = true
                    };

                    user = new User(name: userDTO.Name, email: userDTO.Email, password: userDTO.Password, isGoogleAuthenticated: userDTO.IsGoogleAuthenticated);

                    _databaseContext.Users!.Add(user);
                    await _databaseContext.SaveChangesAsync();
                    _logger.LogInformation("Saved user to database");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userIdClaim),
                    new Claim(ClaimTypes.Email, emailClaim),
                    new Claim(ClaimTypes.Name, nameClaim ?? ""),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                });

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google authentication callback");
                return RedirectToAction("Error", "Home");
            }
        }
        
        [HttpGet("user")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            try
            {
                var user = HttpContext.User;
                
                var userInfo = new
                {
                    Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Email = user.FindFirst(ClaimTypes.Email)?.Value,
                    Name = user.FindFirst(ClaimTypes.Name)?.Value
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user information");
                return StatusCode(500, "An error occurred while retrieving user information");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(){
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}

