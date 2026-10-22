using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptop.Models;

public class ProductPromotion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Range(1, 90)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; } = true;

    public Product Product { get; set; } = null!;

    [NotMapped]
    public bool IsCurrentlyActive
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            return IsActive && StartsAtUtc <= now && EndsAtUtc > now;
        }
    }
}
