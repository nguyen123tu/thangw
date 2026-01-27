namespace MTU.Models.DTOs
{
    public class FriendDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingRequest { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
