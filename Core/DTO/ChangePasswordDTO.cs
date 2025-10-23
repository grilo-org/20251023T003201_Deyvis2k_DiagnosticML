
namespace CSProject.Core.DTO;
public class ChangePasswordDTO
{
    public Guid Id { get; set; }
    public required string NewPassword { get; set; }
}
