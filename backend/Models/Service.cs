using System.ComponentModel.DataAnnotations;
namespace HydroFlowManager.API.Models
{
    public class Service
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal PriceMotorcycle { get; set; }
        public decimal PriceCarSmall { get; set; }
        public decimal PriceCarLarge { get; set; }
        public int DurationMinutes { get; set; }
        public bool Active { get; set; } = true;

        public decimal GetPriceFor(VehicleType t) => t switch
        {
            VehicleType.Motorcycle => PriceMotorcycle,
            VehicleType.CarSmall => PriceCarSmall,
            VehicleType.CarLarge => PriceCarLarge,
            _ => PriceCarSmall
        };
    }
}
