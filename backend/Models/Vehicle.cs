using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HydroFlowManager.API.Models
{
    public enum VehicleType { Motorcycle = 0, CarSmall = 1, CarLarge = 2 }

    public class Vehicle
    {
        [Key]
        public string Plate { get; set; } = null!;

        public VehicleType Type { get; set; }

        // Coluna verdadeira no banco
        public string ClientId { get; set; } = null!;

        public Client? Client { get; set; }

        public List<Order> Orders { get; set; } = new();

        // Só para JSON: frontend manda "clientCpfCnpj"
        [NotMapped]
        [JsonPropertyName("clientCpfCnpj")]
        public string ClientCpfCnpj
        {
            get => ClientId;
            set { ClientId = value; }
        }
    }
}