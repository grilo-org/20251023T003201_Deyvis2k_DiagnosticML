using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CSProject.Data;
using CSProject.Models;
using CSProject.Core.Enums;
using CSProject.Core.DTO;

namespace CSProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly CSProjectContext _dataContext;
    public UserController(CSProjectContext dataContext)
    {
        _dataContext = dataContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDTO registerUserDTO)
    {
        if(!ModelState.IsValid) return BadRequest(ModelState);

        User newUser = new User{
            Name = registerUserDTO.Name,
            Email = registerUserDTO.Email,
            IsGoogleAuthenticated = registerUserDTO.IsGoogleAuthenticated,
        };
        
        Console.WriteLine(registerUserDTO.ToString());
        

        _dataContext.Users!.Add(newUser);
        await _dataContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        var users = await _dataContext.Users!.ToListAsync();
        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _dataContext.Users!.FirstOrDefaultAsync(u => u.Id == id);
        if(user == null) return NotFound();
        return Ok(user);
    }

    [Authorize]
    [HttpPut("changepassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
    {
        if (_dataContext.Users == null)
        {
            return StatusCode(500, "Users table not available");
        }
        
        var user = await _dataContext.Users.FindAsync(changePasswordDTO.Id);
        if (user == null) return NotFound();
        
        if(User?.Identity?.Name != user.Email) return Forbid();

        user.ChangePassword(changePasswordDTO.NewPassword);
        await _dataContext.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _dataContext.Users!.FirstOrDefaultAsync(u => u.Id == id);
        if(user == null) return NotFound();
        _dataContext.Users!.Remove(user);
        await _dataContext.SaveChangesAsync();
        return NoContent();
    }
}
