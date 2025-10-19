using System.ComponentModel.DataAnnotations;
namespace HydroFlowManager.API.Models
{
    public enum VehicleType { Motorcycle = 0, CarSmall = 1, CarLarge = 2 }
    public class Vehicle
    {
        [Key] public string Plate { get; set; } = null!;
        public VehicleType Type { get; set; }
        public string ClientId { get; set; } = null!;
        public Client? Client { get; set; }
        public List<Order> Orders { get; set; } = new();
    }
}
