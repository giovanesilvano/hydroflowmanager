using System.ComponentModel.DataAnnotations;
namespace HydroFlowManager.API.Models
{
    public class Client
    {
        [Key]
        public string CPFCNPJ { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Observations { get; set; }
        public List<Vehicle> Vehicles { get; set; } = new();
    }
}
