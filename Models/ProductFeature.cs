using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptop.Models
{
    public class ProductFeature
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Feature { get; set; } = "";

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}