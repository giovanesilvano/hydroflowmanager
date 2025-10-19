using Microsoft.EntityFrameworkCore;
using HydroFlowManager.API.Models;

namespace HydroFlowManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Attendant> Attendants => Set<Attendant>();
    }
}
