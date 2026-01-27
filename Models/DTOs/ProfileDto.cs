namespace MTU.Models.DTOs
{
    public class ProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? CoverImage { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Interests { get; set; }
        public StudentDto? Student { get; set; }
        public List<PostDto> Posts { get; set; } = new List<PostDto>();
        public List<FriendDto> Friends { get; set; } = new List<FriendDto>();
        public List<FriendDto> SuggestedFriends { get; set; } = new List<FriendDto>();
        public bool IsOwnProfile { get; set; }
        public string? FriendshipStatus { get; set; } // null, "pending", "accepted", "blocked"
        public int FriendCount { get; set; }
    }
}
