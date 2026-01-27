using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;

namespace MTU.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PostService> _logger;

        public PostService(
            IPostRepository postRepository,
            ILikeRepository likeRepository,
            IWebHostEnvironment environment,
            ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<PostDto>> GetFeedAsync(int pageNumber, int pageSize, int? currentUserId = null)
        {
            try
            {
                var posts = await _postRepository.GetAllAsync(pageNumber, pageSize);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feed for page {PageNumber}", pageNumber);
                return new List<PostDto>();            }
        }

        public async Task<List<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null)
        {
            try
            {
                var posts = await _postRepository.GetByUserIdAsync(userId);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts for user {UserId}", userId);
                return new List<PostDto>();            }
        }

        public async Task<PostDto> CreatePostAsync(CreatePostDto dto, int currentUserId)
        {
            try
            {
                // Kiểm tra nội dung
                var content = dto.Content?.Trim();
                var hasContent = !string.IsNullOrEmpty(content);
                var hasImage = dto.Image != null && dto.Image.Length > 0;

                if (!hasContent && !hasImage)
                {
                    throw new ArgumentException("Vui lòng nhập nội dung hoặc chọn ảnh");
                }

                string? imageUrl = null;
                if (hasImage)
                {
                    imageUrl = await SaveImageAsync(dto.Image!);
                }

                var post = new Post
                {
                    UserId = currentUserId,
                    Content = content ?? "",
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Privacy = dto.Privacy ?? "public",
                    IsDeleted = false
                };                var createdPost = await _postRepository.CreateAsync(post);                var postWithUser = await _postRepository.GetByIdAsync(createdPost.PostId);
                
                if (postWithUser == null)
                {
                    throw new InvalidOperationException("Failed to retrieve created post");
                }                var postDtos = await MapPostsToDtos(new List<Post> { postWithUser }, currentUserId);
                return postDtos.First();
            }
            catch (ArgumentException)
            {                throw;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating post for user {UserId}", currentUserId);
                throw new InvalidOperationException("Unable to save post. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user {UserId}", currentUserId);
                throw new InvalidOperationException("An unexpected error occurred while creating the post. Please try again later.");
            }
        }

        public async Task<int> GetTotalPagesAsync(int pageSize)
        {
            try
            {
                var totalCount = await _postRepository.GetTotalCountAsync();
                return (int)Math.Ceiling((double)totalCount / pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total pages");
                return 1;            }
        }

        public async Task<bool> DeletePostAsync(int postId, int currentUserId)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(postId);
                
                if (post == null)
                {
                    _logger.LogWarning("Post {PostId} not found for deletion", postId);
                    return false;
                }                if (post.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {OwnerId}", 
                        currentUserId, postId, post.UserId);
                    return false;
                }                await _postRepository.DeleteAsync(postId);
                
                _logger.LogInformation("Post {PostId} deleted successfully by user {UserId}", postId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                return false;
            }
        }

        private async Task<List<PostDto>> MapPostsToDtos(List<Post> posts, int? currentUserId)
        {
            var postDtos = new List<PostDto>();

            foreach (var post in posts)
            {
                var likeCount = await _postRepository.GetLikeCountAsync(post.PostId);
                var commentCount = await _postRepository.GetCommentCountAsync(post.PostId);
                var isLiked = currentUserId.HasValue 
                    ? await _likeRepository.ExistsAsync(post.PostId, currentUserId.Value)
                    : false;

                var authorName = $"{post.User.FirstName} {post.User.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(authorName))
                {
                    authorName = post.User.Username;
                }

                var postDto = new PostDto
                {
                    PostId = post.PostId,
                    AuthorId = post.UserId,
                    AuthorName = authorName,
                    AuthorAvatar = post.User.Avatar ?? "/assets/user.png",
                    Content = post.Content ?? string.Empty,
                    ImageUrl = post.ImageUrl,
                    TimeAgo = GetTimeAgo(post.CreatedAt),
                    LikeCount = likeCount,
                    CommentCount = commentCount,
                    IsLikedByCurrentUser = isLiked,
                    IsOwnPost = currentUserId.HasValue && post.UserId == currentUserId.Value
                };

                postDtos.Add(postDto);
            }

            return postDtos;
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Only jpg, jpeg, png, and gif are allowed.");
            }            if (image.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds maximum limit of 5MB.");
            }            var uniqueFileName = $"{Guid.NewGuid()}{extension}";            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }            var filePath = Path.Combine(uploadsPath, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }            return $"/uploads/{uniqueFileName}";
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

