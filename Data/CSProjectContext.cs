using Microsoft.EntityFrameworkCore;
using CSProject.Models;

namespace CSProject.Data;


public class CSProjectContext : DbContext
{
    public CSProjectContext(DbContextOptions<CSProjectContext> options) : base(options) { 
        
    }

    public DbSet<User>? Users { get; set; }
}
