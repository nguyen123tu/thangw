namespace MTU.Models.DTOs
{
    public class CommentDto
    {
        public int CommentId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
    }
}
