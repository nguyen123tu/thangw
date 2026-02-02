using MTU.Models.DTOs;
using MTU.Models.Entities;

namespace MTU.Models
{
    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<User> People { get; set; } = new List<User>();
        public List<PostDto> Posts { get; set; } = new List<PostDto>();
        public bool HasResults => People.Any() || Posts.Any();
    }
}
