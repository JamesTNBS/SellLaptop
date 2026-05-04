using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptop.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ImageUrl { get; set; } = "";

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}