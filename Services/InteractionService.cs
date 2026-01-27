using Microsoft.Extensions.Logging;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;

namespace MTU.Services
{
    public class InteractionService : IInteractionService
    {
        private readonly ILikeRepository _likeRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        private readonly ILogger<InteractionService> _logger;

        public InteractionService(
            ILikeRepository likeRepository,
            ICommentRepository commentRepository,
            IPostRepository postRepository,
            ILogger<InteractionService> logger)
        {
            _likeRepository = likeRepository;
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _logger = logger;
        }

        public async Task<LikeResult> ToggleLikeAsync(int postId, int userId)
        {
            try
            {                var existingLike = await _likeRepository.GetAsync(postId, userId);

                if (existingLike != null)
                {                    await _likeRepository.DeleteAsync(existingLike);                    var likeCount = await _postRepository.GetLikeCountAsync(postId);
                    
                    return new LikeResult
                    {
                        IsLiked = false,
                        LikeCount = likeCount
                    };
                }
                else
                {                    var newLike = new Like
                    {
                        PostId = postId,
                        UserId = userId
                    };

                    await _likeRepository.CreateAsync(newLike);                    var likeCount = await _postRepository.GetLikeCountAsync(postId);
                    
                    return new LikeResult
                    {
                        IsLiked = true,
                        LikeCount = likeCount
                    };
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling like for post {PostId} by user {UserId}", postId, userId);
                throw new InvalidOperationException("Unable to update like. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId} by user {UserId}", postId, userId);
                throw new InvalidOperationException("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto, int userId)
        {
            try
            {                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    throw new ArgumentException("Comment content cannot be empty");
                }                var comment = new Comment
                {
                    PostId = dto.PostId,
                    UserId = userId,
                    Content = dto.Content.Trim()
                };                var createdComment = await _commentRepository.CreateAsync(comment);                var comments = await _commentRepository.GetByPostIdAsync(dto.PostId);
                var commentWithUser = comments.FirstOrDefault(c => c.CommentId == createdComment.CommentId);

                if (commentWithUser == null)
                {
                    throw new InvalidOperationException("Failed to retrieve created comment");
                }                return MapCommentToDto(commentWithUser);
            }
            catch (ArgumentException)
            {                throw;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error adding comment to post {PostId} by user {UserId}", dto.PostId, userId);
                throw new InvalidOperationException("Unable to save comment. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post {PostId} by user {UserId}", dto.PostId, userId);
                throw new InvalidOperationException("An unexpected error occurred while adding the comment. Please try again later.");
            }
        }

        public async Task<List<CommentDto>> GetCommentsAsync(int postId)
        {
            try
            {
                var comments = await _commentRepository.GetByPostIdAsync(postId);
                return comments.Select(MapCommentToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments for post {PostId}", postId);
                return new List<CommentDto>();            }
        }

        private CommentDto MapCommentToDto(Comment comment)
        {
            var authorName = $"{comment.User.FirstName} {comment.User.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(authorName))
            {
                authorName = comment.User.Username;
            }

            return new CommentDto
            {
                CommentId = comment.CommentId,
                AuthorName = authorName,
                AuthorAvatar = comment.User.Avatar ?? "/assets/user.png",
                Content = comment.Content,
                TimeAgo = GetTimeAgo(comment.CreatedAt)
            };
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            
            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }
}

