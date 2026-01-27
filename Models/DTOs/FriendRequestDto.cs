namespace MTU.Models.DTOs
{
    public class FriendRequestDto
    {
        public int FriendshipId { get; set; }
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string? RequesterAvatar { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
