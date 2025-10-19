using System.ComponentModel.DataAnnotations;
namespace HydroFlowManager.API.Models
{
    public enum OrderStatus { Open, Paid, Cancelled }
    public enum PaymentMethod { Cash, Pix, CardCredit, CardDebit }

    public class Order
    {
        [Key] public Guid Id { get; set; }
        public string VehiclePlate { get; set; } = null!;
        public Vehicle? Vehicle { get; set; }
        public string? AttendantCPF { get; set; }
        public Attendant? Attendant { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        [Key] public int Id { get; set; }
        public Guid OrderId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
