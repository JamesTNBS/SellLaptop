using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptop.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }           // FK to User.Id

        public int? ParentCommentId { get; set; }

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ParentCommentId")]
        public Comment? ParentComment { get; set; }

        public List<Comment> Replies { get; set; } = new();
    }
}
