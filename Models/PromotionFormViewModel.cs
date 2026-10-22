using System.ComponentModel.DataAnnotations;

namespace Laptop.Models;

public class PromotionFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "PromotionProduct")]
    public int ProductId { get; set; }

    [Range(1, 90)]
    [Display(Name = "DiscountPercent")]
    public decimal DiscountPercent { get; set; } = 10;

    [Required]
    [Display(Name = "PromotionStartsAt")]
    public DateTime StartsAtLocal { get; set; } = DateTime.UtcNow.AddHours(7).AddMinutes(10);

    [Required]
    [Display(Name = "PromotionEndsAt")]
    public DateTime EndsAtLocal { get; set; } = DateTime.UtcNow.AddHours(7).AddDays(7);

    [Display(Name = "PromotionActive")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "PromotionFeatured")]
    public bool IsFeatured { get; set; } = true;
}

public class PromotionManagementViewModel
{
    public PromotionFormViewModel Form { get; set; } = new();

    public List<ProductPromotion> Promotions { get; set; } = new();

    public List<Product> Products { get; set; } = new();
}
