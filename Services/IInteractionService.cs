using MTU.Models.DTOs;

namespace MTU.Services
{
    public interface IInteractionService
    {
        Task<LikeResult> ToggleLikeAsync(int postId, int userId);
        Task<CommentDto> AddCommentAsync(CreateCommentDto dto, int userId);
        Task<List<CommentDto>> GetCommentsAsync(int postId);
    }
}

