using CSProject.Core.Enums;
using System.ComponentModel.DataAnnotations;
namespace CSProject.Core.DTO;

public class RegisterUserDTO
{
    public required string Name { get; set; }
    
    [EmailAddress(ErrorMessage = "O endereço de e-mail fornecido não é válido.")]
    public required string Email { get; set; }
    
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial.")]
    public required string Password { get; set; }
    public required bool IsGoogleAuthenticated { get; set; }


    public override string ToString() =>
        $"Name: {Name}, Email: {Email}, Password: {BCrypt.Net.BCrypt.HashPassword(Password)}, IsGoogleAuthenticated: {IsGoogleAuthenticated}";
}
