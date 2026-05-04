using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string Title { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Image { get; set; } = string.Empty;

        public Order Order { get; set; } = null!;
    }
}
