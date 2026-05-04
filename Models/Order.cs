using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string StateOrProvince { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
