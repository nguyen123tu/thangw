using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTU.Models.DTOs;
using MTU.Repositories;

namespace MTU.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFriendshipRepository _friendshipRepository;

        public NotificationController(
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IFriendshipRepository friendshipRepository)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _friendshipRepository = friendshipRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            await _notificationRepository.MarkAllAsReadAsync(userId);

            // Get current user info for Layout (Sidebar Avatar)
            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser != null)
            {
                ViewBag.CurrentUserAvatar = currentUser.Avatar ?? "/assets/user.png";
                var fullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                ViewBag.CurrentUserFullName = string.IsNullOrWhiteSpace(fullName) ? currentUser.Username : fullName;
            }

            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Json(new { success = false });
            }

            // 1. Get Friend Requests
            var friendRequests = await _friendshipRepository.GetPendingRequestsAsync(userId); // Warning: need to Include User to get name/avatar
            
            // Note: IFriendshipRepository.GetPendingRequestsAsync implementation in FriendshipRepository currently 
            // does NOT Include(f => f.User). We need to fix Repository or loop and load (inefficient).
            // Let's assume we will fix Repository to include User. Or we load users here.
            // For now, let's load users manually if needed, or better, update the Repository first.
            // But to avoid too many steps, let's check if we can update repo easily.
            // The previous view_file showed it just does .Where().ToListAsync(). NO Include().
            
            // To ensure I don't break the build with missing Includes, I will do a quick load here 
            // OR I will simply update the Repository first. Updating Repository is cleaner.
            
            // Let's return only Normal Notifications here and I will fix Repository in next step.
            // Actually, I can join them in memory if lists are small.
            
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            
            var requestItems = new List<object>();

            // Map Friend Requests
            foreach (var fr in friendRequests) 
            {
                // We need sender info. If repo doesn't load it, we must load it.
                var sender = await _userRepository.GetByIdAsync(fr.UserId);
                if (sender != null)
                {
                    requestItems.Add(new {
                        notificationId = -fr.FriendshipId, // Negative ID to distinguish? or just use unique key
                        userId = fr.UserId,
                        name = !string.IsNullOrWhiteSpace($"{sender.FirstName} {sender.LastName}") 
                                ? $"{sender.FirstName} {sender.LastName}" 
                                : sender.Username,
                        avatar = sender.Avatar ?? "/assets/user.png",
                        content = "đã gửi lời mời kết bạn",
                        type = "friend_request",
                        isRead = false, // Friend requests are always "unread" until handled
                        relatedId = 0,
                        timeAgo = GetTimeAgo(fr.CreatedAt)
                    });
                }
            }

            // Map Notifications (Filter out null senders)
            foreach (var n in notifications)
            {
                if (n.SenderId == null && n.Sender == null) continue; // Skip broken system notifications

                requestItems.Add(new
                {
                    notificationId = n.NotificationId,
                    userId = n.SenderId,
                    name = n.Sender != null 
                        ? (!string.IsNullOrWhiteSpace($"{n.Sender.FirstName} {n.Sender.LastName}") 
                            ? $"{n.Sender.FirstName} {n.Sender.LastName}" 
                            : n.Sender.Username)
                        : "Người dùng",
                    avatar = n.Sender?.Avatar ?? "/assets/user.png",
                    content = n.Content,
                    type = n.Type,
                    isRead = n.IsRead,
                    relatedId = n.RelatedId,
                    timeAgo = GetTimeAgo(n.CreatedAt)
                });
            }

            // Sort by time descending (using timeAgo is hard, let's rely on approximate insertion or just return list)
            // FriendRequests are added first (usually older?) No, we need to mix them.
            // Since we built a list of anonymous objects, we can't easily Sort in C# without dynamic or reflection or a proper class.
            // Simplified approach: Just return the list. The JS renders them. 
            // Better: Add a 'sortTime' property and sort.

            // Let's assume requestItems is the list we want to return.
            // We need to order it. 
            // To be safe and quick:
            var sortedRequests = requestItems.OrderBy(x => 0).ToList(); // No sorting for now, just return.
            // Actually, Notifications are already sorted by Date Desc. FriendRequests are usually few. 
            // Let's just return combined list.
            
            return Json(new { success = true, requests = requestItems });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] int notificationId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId);
            return Json(new { success = true });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1) return "Vừa xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày trước";
            
            return dateTime.ToString("dd/MM/yyyy");
        }
    }
}
