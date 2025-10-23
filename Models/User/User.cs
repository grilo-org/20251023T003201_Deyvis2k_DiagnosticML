using System.ComponentModel.DataAnnotations;
using CSProject.Core.Enums;


namespace CSProject.Models;

public class User
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; private set; } = string.Empty;

    public UserRole Role { get; private set; } = UserRole.Client;

    public bool IsGoogleAuthenticated { get; set; }

    private const int PasswordWorkFactor = 12;

    public User() { }

    public User(string name, string email, string password, bool isGoogleAuthenticated = false) { 
        Name = name;
        Email = email;
        ValidatePassword(password);
        Password = HashPassword(password);
        IsGoogleAuthenticated = isGoogleAuthenticated;
    }

    public bool VerifyPassword(string password) =>
        BCrypt.Net.BCrypt.Verify(password, Password);

    public void ChangePassword(string newPassword)
    {
        ValidatePassword(newPassword);
        Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: PasswordWorkFactor);
    }

    private static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: PasswordWorkFactor);

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A senha não pode estar vazia.");
            
        if (password.Length < 8)
            throw new ArgumentException("A senha deve ter pelo menos 8 caracteres.");
            
        if (!password.Any(char.IsUpper))
            throw new ArgumentException("A senha deve conter pelo menos uma letra maiúscula.");
            
        if (!password.Any(char.IsLower))
            throw new ArgumentException("A senha deve conter pelo menos uma letra minúscula.");
            
        if (!password.Any(char.IsDigit))
            throw new ArgumentException("A senha deve conter pelo menos um número.");
            
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("A senha deve conter pelo menos um caractere especial.");
    }
}
