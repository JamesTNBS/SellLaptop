using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class CheckoutViewModel
    {
        [Required]
        [Display(Name = "FullName")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "AddressLine1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [Display(Name = "AddressLine2")]
        public string AddressLine2 { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required]
        [Display(Name = "StateOrProvince")]
        public string StateOrProvince { get; set; } = string.Empty;

        [Required]
        [Display(Name = "PostalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "PaymentMethod")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        [Display(Name = "OrderNotes")]
        public string Notes { get; set; } = string.Empty;

        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(x => x.Price * x.Quantity);

        public int ItemCount => Items.Sum(x => x.Quantity);
    }
}
