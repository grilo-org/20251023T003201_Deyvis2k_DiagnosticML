using CSProject.Core.Enums;

namespace CSProject.Models;
public class Client : User
{
    public new UserRole Role => UserRole.Client;
    public Client(string name, string email, string password) : 
        base(name, email, password) { }
}
