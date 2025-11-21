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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keys
            modelBuilder.Entity<Client>().HasKey(c => c.CPFCNPJ);
            modelBuilder.Entity<Vehicle>().HasKey(v => v.Plate);
            modelBuilder.Entity<Service>().HasKey(s => s.Id);
            modelBuilder.Entity<Attendant>().HasKey(a => a.CPF);
            modelBuilder.Entity<Order>().HasKey(o => o.Id);
            modelBuilder.Entity<OrderItem>().HasKey(oi => oi.Id);

            // Relationships
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Client)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.ClientId)
                .HasPrincipalKey(c => c.CPFCNPJ)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Vehicle)
                .WithMany(v => v.Orders)
                .HasForeignKey(o => o.VehiclePlate)
                .HasPrincipalKey(v => v.Plate)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Attendant)
                .WithMany()
                .HasForeignKey(o => o.AttendantCPF)
                .HasPrincipalKey(a => a.CPF)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne<Order>()
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne<Service>()
                .WithMany()
                .HasForeignKey(oi => oi.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
