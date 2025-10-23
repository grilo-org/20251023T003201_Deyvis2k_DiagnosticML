using CSProject.Core.Enums;

namespace CSProject.Models;
public class Admin : User
{
    public new UserRole Role => UserRole.Admin;
    public Admin(string name, string email, string password) : 
        base(name, email, password) { }
}
