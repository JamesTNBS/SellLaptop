using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptop.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; } = string.Empty;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}