using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required")]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        // Temporary fields for Create/Edit form only
        [Display(Name = "Key Features (one per line)")]
        public string Features { get; set; } = string.Empty;

        [Display(Name = "Images (comma-separated URLs)")]
        public string? Images { get; set; }

        [Required(ErrorMessage = "Short Description is required")]
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
