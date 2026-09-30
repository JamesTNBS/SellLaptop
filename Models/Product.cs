using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        // Temporary fields for Create/Edit form only
        [Display(Name = "Key Features (one per line)")]
        public string Features { get; set; } = string.Empty;

        [Display(Name = "Images (comma-separated URLs)")]
        public string? Images { get; set; }

        [Required]
        [Display(Name = "Short Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Full Description")]
        public string FullDescription { get; set; } = string.Empty;

        public string Seller { get; set; } = "Admin";
        public string Condition { get; set; } = "Like New";

        // Navigation Properties (Recommended names)
        public List<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public List<ProductFeature> ProductFeatures { get; set; } = new List<ProductFeature>();
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
