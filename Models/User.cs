using System.ComponentModel.DataAnnotations;

namespace Laptop.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        // Navigation Properties
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
