namespace CSProject.Core.DTO;

public class LoginDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; } = false;
}
