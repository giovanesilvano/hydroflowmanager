using HydroFlowManager.API.Models;
namespace HydroFlowManager.API.DTOs
{
    public class OrderItemDto
    {
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
    }
    public class OrderCreateDto
    {
        public string VehiclePlate { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new();
        public PaymentMethod PaymentMethod { get; set; }
        public string? AttendantCPF { get; set; }
    }

    public class OrderUpdateDto
    {
        public int PaymentMethod { get; set; }           // <- int em vez de enum
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderPaymentDto
    {
        public PaymentMethod PaymentMethod { get; set; }
    }
}
