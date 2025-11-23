using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
        [JsonIgnore]  // ✅ Não serializa a lista de veículos quando retornar Client
        public List<Vehicle> Vehicles { get; set; } = new();

        [JsonIgnore]  // ✅ Também ignora Orders se tiver
        public List<Order> Orders { get; set; } = new();
    }
}
