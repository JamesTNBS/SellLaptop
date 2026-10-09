using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

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
        [Display(Name = "Specifications")]
        public string Features { get; set; } = string.Empty;

        [NotMapped]
        [Display(Name = "Specifications")]
        public string Specifications
        {
            get => Features;
            set => Features = value ?? string.Empty;
        }

        [Display(Name = "Technical specifications")]
        public string TechnicalSpecifications { get; set; } = string.Empty;

        public const string SpecificationsTemplate = """
            Graphics card type:
            RAM capacity:
            RAM type:
            Number of RAM slots:
            Storage:
            Display technology:
            Operating system:
            CPU type:
            Screen size:
            Screen resolution:
            Battery:
            Communication port:
            """;

        public const string TechnicalSpecificationsTemplate = """
            Configuration & Memory
            Graphics card type:
            Operating system upon release:
            CPU type:

            RAM
            RAM capacity:
            RAM type:
            Number of RAM slots:
            Storage:

            Screen
            Scanning frequency:
            Substrate material:
            Display technology:
            Screen size:
            Screen resolution:

            Sound
            Audio technology:

            Size & Weight
            Material:
            Screen casing material:
            Top shell material:
            Bottom shell material:

            Other amenities
            Special features:

            Other features
            Keyboard backlight type:
            Security:
            Webcam:

            Batteries & Charging Technology
            Battery:

            Communication & Connection
            Wi-Fi:
            Bluetooth:

            Design & Materials
            Size:
            Weight:

            Connection port
            Communication port:
            """;

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
