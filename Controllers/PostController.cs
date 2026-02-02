using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTU.Models.DTOs;
using MTU.Services;

namespace MTU.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly IInteractionService _interactionService;
        private readonly IAuthService _authService;
        private readonly ILogger<PostController> _logger;

        public PostController(
            IPostService postService,
            IInteractionService interactionService,
            IAuthService authService,
            ILogger<PostController> logger)
        {
            _postService = postService;
            _interactionService = interactionService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Quản lý đăng bài gồm hình ảnh
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    _logger.LogWarning("User ID not found in authentication context");
                    return Unauthorized();
                }

                // Debug log
                _logger.LogInformation("Create post - Content: '{Content}', HasImage: {HasImage}, HasVideo: {HasVideo}, HasFile: {HasFile}", 
                    dto.Content ?? "(null)", dto.Image != null, dto.Video != null, dto.Attachment != null);

                // Kiểm tra nội dung - trim để loại bỏ khoảng trắng
                var content = dto.Content?.Trim();
                var hasContent = !string.IsNullOrEmpty(content);
                var hasImage = dto.Image != null && dto.Image.Length > 0;
                var hasVideo = dto.Video != null && dto.Video.Length > 0;
                var hasAttachment = dto.Attachment != null && dto.Attachment.Length > 0;

                if (!hasContent && !hasImage && !hasVideo && !hasAttachment)
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập nội dung, hoặc tải ảnh/video/tài liệu lên";
                    return RedirectToAction("Index", "Home");
                }

                // Cập nhật content đã trim
                dto.Content = content;

                var createdPost = await _postService.CreatePostAsync(dto, currentUserId);

                _logger.LogInformation("Post created successfully by user {UserId}: PostId {PostId}", 
                    currentUserId, createdPost.PostId);

                TempData["SuccessMessage"] = "Post created successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating post");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                TempData["ErrorMessage"] = "Unable to create post. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Like và gỡ like (Ajax) (Chưa biết được ai like)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Like(int postId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    _logger.LogWarning("User ID not found in authentication context for like action");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _interactionService.ToggleLikeAsync(postId, currentUserId);

                _logger.LogInformation("User {UserId} toggled like on post {PostId}: IsLiked={IsLiked}", 
                    currentUserId, postId, result.IsLiked);

                return Json(new 
                { 
                    success = true, 
                    isLiked = result.IsLiked, 
                    likeCount = result.LikeCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId}", postId);
                return Json(new { success = false, message = "Unable to process like action" });
            }
        }

        /// <summary>
        /// Tạo bình luận thông qua Ajax
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Comment([FromBody] CreateCommentDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    _logger.LogWarning("User ID not found in authentication context for comment action");
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
                {
                    return Json(new { success = false, message = "Nội dung bình luận không được để trống" });
                }

                var comment = await _interactionService.AddCommentAsync(dto, currentUserId);

                _logger.LogInformation("User {UserId} added comment to post {PostId}: CommentId {CommentId}", 
                    currentUserId, dto.PostId, comment.CommentId);

                return Json(new 
                { 
                    success = true, 
                    comment = new
                    {
                        commentId = comment.CommentId,
                        authorName = comment.AuthorName,
                        authorAvatar = comment.AuthorAvatar,
                        content = comment.Content,
                        timeAgo = comment.TimeAgo
                    }
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error adding comment");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post {PostId}", dto?.PostId);
                return Json(new { success = false, message = "Không thể thêm bình luận" });
            }
        }

        /// <summary>
        /// Xem bình luận bài post
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComments(int postId)
        {
            try
            {
                var comments = await _interactionService.GetCommentsAsync(postId);

                _logger.LogInformation("Retrieved {Count} comments for post {PostId}", 
                    comments.Count, postId);

                return Json(new 
                { 
                    success = true, 
                    comments = comments.Select(c => new
                    {
                        commentId = c.CommentId,
                        authorName = c.AuthorName,
                        authorAvatar = c.AuthorAvatar,
                        content = c.Content,
                        timeAgo = c.TimeAgo
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments for post {PostId}", postId);
                return Json(new { success = false, message = "Unable to retrieve comments" });
            }
        }

        /// <summary>
        /// Xóa bài
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var result = await _postService.DeletePostAsync(id, currentUserId);

                if (result)
                {
                    _logger.LogInformation("Post {PostId} deleted by user {UserId}", id, currentUserId);
                    return Json(new { success = true, message = "Đã xóa bài viết" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa bài viết. Bạn không có quyền hoặc bài viết không tồn tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bài viết" });
            }
        }
        /// <summary>
        /// Xem chi tiết bài viết
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                currentUserId = 0; 
                return RedirectToAction("Login", "Home");
            }

            // Populate Sidebar User Data
            var currentUser = await _authService.GetUserByIdAsync(currentUserId);
            if (currentUser != null)
            {
                ViewBag.CurrentUserAvatar = currentUser.Avatar ?? "/assets/user.png";
                var fullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                ViewBag.CurrentUserFullName = string.IsNullOrWhiteSpace(fullName) ? currentUser.Username : fullName;
            }

            var post = await _postService.GetPostByIdAsync(id, currentUserId);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePrivacy(int postId, string privacy)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                var result = await _postService.UpdatePrivacyAsync(postId, privacy, currentUserId);

                if (result)
                {
                    return Json(new { success = true, message = "Đã cập nhật quyền riêng tư" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật quyền riêng tư. Bạn không có quyền hoặc bài viết không tồn tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy for post {PostId}", postId);
                return Json(new { success = false, message = "Lỗi hệ thống" });
            }
        }


        /// <summary>
        /// Lưu hoặc bỏ lưu bài viết (Ajax)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Save(int postId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var isSaved = await _interactionService.ToggleSaveAsync(postId, currentUserId);

                return Json(new { success = true, isSaved = isSaved });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bật/tắt lưu bài viết với PostId = {PostId}", postId);
                return Json(new { success = false, message = "Không thể xử lý thao tác lưu bài viết" });

            }
        }

        /// <summary>
        /// Xem danh sách bài viết đã lưu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Saved()
        {
             var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                return RedirectToAction("Login", "Home");
            }

            // Populate Sidebar User Data
            var currentUser = await _authService.GetUserByIdAsync(currentUserId);
            if (currentUser != null)
            {
                ViewBag.CurrentUserAvatar = currentUser.Avatar ?? "/assets/user.png";
                var fullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                ViewBag.CurrentUserFullName = string.IsNullOrWhiteSpace(fullName) ? currentUser.Username : fullName;
            }

            var savedPosts = await _postService.GetSavedPostsAsync(1, 20, currentUserId);
            return View(savedPosts);
        }
    }
}
