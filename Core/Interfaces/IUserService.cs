using System.Threading;
using CSProject.Models;

namespace CSProject.Core.Interfaces;
    
public interface IUserService
{
    Task<User> GetCurrentUserAsync();
}
