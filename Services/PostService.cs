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
        private readonly ISavedPostRepository _savedPostRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PostService> _logger;


        public PostService(
            IPostRepository postRepository,
            ILikeRepository likeRepository,
            ISavedPostRepository savedPostRepository,
            IWebHostEnvironment environment,
            ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
            _savedPostRepository = savedPostRepository;
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

                if (!hasContent && !hasImage && dto.Video == null && dto.Attachment == null)
                {
                    throw new ArgumentException("Vui lòng nhập nội dung, hoặc tải ảnh/video/tài liệu lên");
                }

                string? imageUrl = null;
                if (hasImage)
                {
                    imageUrl = await SaveImageAsync(dto.Image!);
                }

                string? videoUrl = null;
                if (dto.Video != null && dto.Video.Length > 0)
                {
                    videoUrl = await SaveVideoAsync(dto.Video);
                }

                string? fileUrl = null;
                string? fileName = null;
                if (dto.Attachment != null && dto.Attachment.Length > 0)
                {
                    var fileInfo = await SaveFileAsync(dto.Attachment);
                    fileUrl = fileInfo.Url;
                    fileName = fileInfo.Name;
                }

                var post = new Post
                {
                    UserId = currentUserId,
                    Content = content ?? "",
                    ImageUrl = imageUrl,
                    VideoUrl = videoUrl,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Privacy = dto.Privacy ?? "public",
                    IsDeleted = false
                };
                
                var createdPost = await _postRepository.CreateAsync(post);
                var postWithUser = await _postRepository.GetByIdAsync(createdPost.PostId);
                
                if (postWithUser == null)
                {
                    throw new InvalidOperationException("Failed to retrieve created post");
                }
                
                var postDtos = await MapPostsToDtos(new List<Post> { postWithUser }, currentUserId);
                return postDtos.First();
            }
            catch (ArgumentException)
            {
                throw;
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
                }

                if (post.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {OwnerId}", 
                        currentUserId, postId, post.UserId);
                    return false;
                }

                await _postRepository.DeleteAsync(postId);
                
                _logger.LogInformation("Post {PostId} deleted successfully by user {UserId}", postId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                return false;
            }
        }

        public async Task<List<PostDto>> GetFriendFeedAsync(int pageNumber, int pageSize, int currentUserId)
        {
            try
            {
                var posts = await _postRepository.GetFriendPostsAsync(currentUserId, pageNumber, pageSize);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving friend feed for user {UserId}", currentUserId);
                return new List<PostDto>();
            }
        }

        public async Task<List<PostDto>> GetExploreFeedAsync(int pageNumber, int pageSize, int currentUserId)
        {
            try
            {
                var posts = await _postRepository.GetExplorePostsAsync(currentUserId, pageNumber, pageSize);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving explore feed for user {UserId}", currentUserId);
                return new List<PostDto>();
            }


        }

        public async Task<List<PostDto>> GetSavedPostsAsync(int pageNumber, int pageSize, int currentUserId)
        {
            try
            {
                var posts = await _savedPostRepository.GetSavedPostsAsync(currentUserId, pageNumber, pageSize);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving saved posts for user {UserId}", currentUserId);
                return new List<PostDto>();
            }
        }

        private async Task<List<PostDto>> MapPostsToDtos(List<Post> posts, int? currentUserId)
        {
            var postDtos = new List<PostDto>();
            HashSet<int> savedPostIds = new HashSet<int>();
            
            if (currentUserId.HasValue)
            {
                var ids = await _savedPostRepository.GetSavedPostIdsAsync(currentUserId.Value);
                savedPostIds = new HashSet<int>(ids);
            }

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
                    VideoUrl = post.VideoUrl,
                    FileUrl = post.FileUrl, 
                    FileName = post.FileName,
                    TimeAgo = GetTimeAgo(post.CreatedAt),
                    LikeCount = likeCount,
                    CommentCount = commentCount,
                    IsLikedByCurrentUser = isLiked,
                    IsSavedByCurrentUser = savedPostIds.Contains(post.PostId),
                    IsOwnPost = currentUserId.HasValue && post.UserId == currentUserId.Value
                };

                postDtos.Add(postDto);
            }

            return postDtos;
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid image type. Only jpg, jpeg, png, gif, and webp are allowed.");
            }
            
            if (image.Length > 10 * 1024 * 1024) // 10MB limit
            {
                throw new ArgumentException("Image size exceeds maximum limit of 10MB.");
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "images");
            
            // Ensure directory exists
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/images/{uniqueFileName}";
        }

        private async Task<string> SaveVideoAsync(IFormFile video)
        {
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var extension = Path.GetExtension(video.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid video type. Only mp4, mov, avi, mkv, and webm are allowed.");
            }
            
            if (video.Length > 100 * 1024 * 1024) // 100MB limit
            {
                throw new ArgumentException("Video size exceeds maximum limit of 100MB.");
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "videos");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await video.CopyToAsync(stream);
            }

            return $"/uploads/videos/{uniqueFileName}";
        }

        private async Task<(string Url, string Name)> SaveFileAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file type ({extension}). Allowed types: pdf, office docs, txt, zip, rar.");
            }
            
            if (file.Length > 20 * 1024 * 1024) // 20MB limit
            {
                throw new ArgumentException("File size exceeds maximum limit of 20MB.");
            }

            // Keep original filename but ensure uniqueness prefix
            var cleanFileName = Path.GetFileName(file.FileName); // simple check
            var uniqueFileName = $"{Guid.NewGuid()}_{cleanFileName}";
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "files");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ($"/uploads/files/{uniqueFileName}", cleanFileName);
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

        public async Task<List<PostDto>> SearchPostsAsync(string query, int currentUserId, int limit = 20)
        {
            try
            {
                var posts = await _postRepository.SearchAsync(query, limit);
                return await MapPostsToDtos(posts, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts with query '{Query}'", query);
                return new List<PostDto>();
            }
        }
        public async Task<PostDto?> GetPostByIdAsync(int postId, int currentUserId)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null) return null;

                var dtos = await MapPostsToDtos(new List<Post> { post }, currentUserId);
                return dtos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post {PostId}", postId);
                return null;
            }
        }

        public async Task<bool> UpdatePrivacyAsync(int postId, string privacy, int currentUserId)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null) return false;

                if (post.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to update privacy of post {PostId} owner by {OwnerId}", 
                        currentUserId, postId, post.UserId);
                    return false;
                }

                if (privacy != "public" && privacy != "friends" && privacy != "private")
                {
                    return false;
                }

                await _postRepository.UpdatePrivacyAsync(postId, privacy);
                _logger.LogInformation("Privacy of post {PostId} updated to {Privacy} by user {UserId}", postId, privacy, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy for post {PostId}", postId);
                return false;
            }
        }
    }
}

