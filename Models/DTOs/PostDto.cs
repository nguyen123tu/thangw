namespace MTU.Models.DTOs
{
    public class PostDto
    {
        public int PostId { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsSavedByCurrentUser { get; set; }
        public bool IsOwnPost { get; set; }
    }
}
