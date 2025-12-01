using Microsoft.EntityFrameworkCore;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Database;

public class MnemaDataContext(DbContextOptions options): DbContext(options)
{
    
    public DbSet<MnemaUser> Users { get; set; }
    
    public DbSet<Subscription> Subscriptions { get; set; }
    
}