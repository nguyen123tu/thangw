namespace MTU.Models
{
    public class StoryViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Index { get; set; }
    }
}
