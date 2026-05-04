using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province or state is required")]
        public string StateOrProvince { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a payment method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        public string Notes { get; set; } = string.Empty;

        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(x => x.Price * x.Quantity);

        public int ItemCount => Items.Sum(x => x.Quantity);
    }
}
