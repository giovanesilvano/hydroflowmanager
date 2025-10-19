using System.ComponentModel.DataAnnotations;
namespace HydroFlowManager.API.Models
{
    public class Attendant
    {
        [Key] public string CPF { get; set; } = null!;
        public string Name { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
    }
}
