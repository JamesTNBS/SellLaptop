namespace Laptop.Models.DTOs
{
    public class CommentDTO
    {
        public int ProductId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
