using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MTU.Data;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;

namespace MTU.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository;
        private readonly MTUSocialDbContext _context;
        private readonly ILogger<FriendshipService> _logger;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IUserRepository userRepository,
            MTUSocialDbContext context,
            ILogger<FriendshipService> logger)
        {
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<List<FriendDto>> GetFriendsAsync(int userId)
        {
            try
            {
                var friendships = await _friendshipRepository.GetFriendsAsync(userId);
                var friendDtos = new List<FriendDto>();

                foreach (var friendship in friendships)
                {
                    var friendId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;
                    var friendUser = await _userRepository.GetByIdAsync(friendId);

                    if (friendUser != null)
                    {
                        var fullName = $"{friendUser.FirstName} {friendUser.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            fullName = friendUser.Username;
                        }

                        friendDtos.Add(new FriendDto
                        {
                            UserId = friendUser.UserId,
                            FullName = fullName,
                            Avatar = friendUser.Avatar ?? "/assets/user.png",
                            Bio = friendUser.Bio,
                            IsFriend = true,
                            HasPendingRequest = false,
                            MutualFriendsCount = 0 
                        });
                    }
                }

                return friendDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends for user {UserId}", userId);
                return new List<FriendDto>();
            }
        }        public async Task<List<FriendDto>> GetFriendSuggestionsAsync(int currentUserId)
        {
            try
            {
                var suggestedUsers = await _userRepository.GetSuggestedFriendsAsync(currentUserId, 10);

                // Lấy pending requests một lần duy nhất (tránh N+1)
                var pendingTargetIds = (await _context.Friendships
                    .Where(f => f.UserId == currentUserId && f.Status == "pending")
                    .Select(f => f.FriendId)
                    .ToListAsync()).ToHashSet();

                var friendDtos = new List<FriendDto>();

                foreach (var user in suggestedUsers)
                {
                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(fullName))
                        fullName = user.Username;

                    // Số bạn chung thật sự
                    var mutualCount = await _userRepository.GetMutualFriendCountAsync(currentUserId, user.UserId);

                    // Đã gửi pending hay chưa
                    var hasPending = pendingTargetIds.Contains(user.UserId);

                    // Bio hiển thị: ưu tiên class/major nếu có
                    var displayBio = user.Bio;
                    if (string.IsNullOrWhiteSpace(displayBio) && user.Student != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrEmpty(user.Student.Class))
                            parts.Add($"Lớp {user.Student.Class}");
                        if (!string.IsNullOrEmpty(user.Student.Faculty))
                            parts.Add(user.Student.Faculty);
                        displayBio = string.Join(" · ", parts);
                    }

                    friendDtos.Add(new FriendDto
                    {
                        UserId            = user.UserId,
                        FullName          = fullName,
                        Avatar            = user.Avatar ?? "/assets/user.png",
                        Bio               = displayBio,
                        IsFriend          = false,
                        HasPendingRequest = hasPending,
                        MutualFriendsCount = mutualCount
                    });
                }

                return friendDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend suggestions for user {UserId}", currentUserId);
                return new List<FriendDto>();
            }
        }
        public async Task<List<FriendRequestDto>> GetPendingRequestsAsync(int userId)
        {
            try
            {
                var pendingRequests = await _friendshipRepository.GetPendingRequestsAsync(userId);
                var requestDtos = new List<FriendRequestDto>();

                foreach (var request in pendingRequests)
                {
                    var requester = await _userRepository.GetByIdAsync(request.UserId);

                    if (requester != null)
                    {
                        var requesterName = $"{requester.FirstName} {requester.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(requesterName))
                        {
                            requesterName = requester.Username;
                        }

                        requestDtos.Add(new FriendRequestDto
                        {
                            FriendshipId = request.FriendshipId,
                            RequesterId = request.UserId,
                            RequesterName = requesterName,
                            RequesterAvatar = requester.Avatar ?? "/assets/user.png",
                            RequestedAt = request.CreatedAt
                        });
                    }
                }

                return requestDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests for user {UserId}", userId);
                return new List<FriendRequestDto>();
            }
        }        public async Task<FriendshipResult> SendFriendRequestAsync(int userId, int friendId)
        {
            try
            {                if (userId == friendId)
                {
                    return new FriendshipResult
                    {
                        Success = false,
                        ErrorMessage = "Không thể gửi lời mời kết bạn cho chính mình"
                    };
                }                var existing = await _friendshipRepository.GetFriendshipAsync(userId, friendId);
                if (existing != null)
                {
                    if (existing.Status == "accepted")
                    {
                        return new FriendshipResult
                        {
                            Success = false,
                            ErrorMessage = "Bạn đã là bạn bè với người này"
                        };
                    }

                    if (existing.Status == "pending")
                    {
                        return new FriendshipResult
                        {
                            Success = false,
                            ErrorMessage = "Lời mời kết bạn đã được gửi trước đó"
                        };
                    }
                }                var friendship = new Friendship
                {
                    UserId = userId,
                    FriendId = friendId,
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _friendshipRepository.CreateAsync(friendship);                var notification = new Notification
                {
                    UserId = friendId,
                    Type = "friend_request",
                    Content = "đã gửi lời mời kết bạn",
                    RelatedId = userId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} sent friend request to {FriendId}", userId, friendId);

                return new FriendshipResult
                {
                    Success = true,
                    Status = "pending"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending friend request from {UserId} to {FriendId}", userId, friendId);
                return new FriendshipResult
                {
                    Success = false,
                    ErrorMessage = "Không thể gửi lời mời. Vui lòng thử lại"
                };
            }
        }        public async Task<FriendshipResult> AcceptFriendRequestAsync(int userId, int friendId)
        {
            try
            {                var friendship = await _friendshipRepository.GetFriendshipAsync(friendId, userId);

                if (friendship == null)
                {
                    return new FriendshipResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy lời mời kết bạn"
                    };
                }

                if (friendship.Status != "pending")
                {
                    return new FriendshipResult
                    {
                        Success = false,
                        ErrorMessage = "Lời mời kết bạn không hợp lệ"
                    };
                }                friendship.Status = "accepted";
                friendship.UpdatedAt = DateTime.Now;
                await _friendshipRepository.UpdateAsync(friendship);                var notification = new Notification
                {
                    UserId = friendId,
                    Type = "friend_accept",
                    Content = "đã chấp nhận lời mời kết bạn",
                    RelatedId = userId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} accepted friend request from {FriendId}", userId, friendId);

                return new FriendshipResult
                {
                    Success = true,
                    Status = "accepted"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request from {FriendId} by {UserId}", friendId, userId);
                return new FriendshipResult
                {
                    Success = false,
                    ErrorMessage = "Không thể chấp nhận lời mời. Vui lòng thử lại"
                };
            }
        }        public async Task<FriendshipResult> DeclineFriendRequestAsync(int userId, int friendId)
        {
            try
            {                var friendship = await _friendshipRepository.GetFriendshipAsync(friendId, userId);

                if (friendship == null)
                {
                    return new FriendshipResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy lời mời kết bạn"
                    };
                }

                if (friendship.Status != "pending")
                {
                    return new FriendshipResult
                    {
                        Success = false,
                        ErrorMessage = "Lời mời kết bạn không hợp lệ"
                    };
                }                await _friendshipRepository.DeleteAsync(friendship);

                _logger.LogInformation("User {UserId} declined friend request from {FriendId}", userId, friendId);

                return new FriendshipResult
                {
                    Success = true,
                    Status = "declined"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining friend request from {FriendId} by {UserId}", friendId, userId);
                return new FriendshipResult
                {
                    Success = false,
                    ErrorMessage = "Không thể từ chối lời mời. Vui lòng thử lại"
                };
            }
        }        public async Task<int> GetFriendCountAsync(int userId)
        {
            try
            {
                var friendIds = await _friendshipRepository.GetFriendIdsAsync(userId);
                return friendIds.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend count for user {UserId}", userId);
                return 0;
            }
        }        public async Task<List<object>> GetPendingFriendRequestsAsync(int userId)
        {
            try
            {
                var pendingRequests = await _friendshipRepository.GetPendingRequestsAsync(userId);
                var result = new List<object>();

                foreach (var request in pendingRequests)
                {
                    var requester = await _userRepository.GetByIdAsync(request.UserId);
                    if (requester != null)
                    {
                        var fullName = $"{requester.FirstName} {requester.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            fullName = requester.Username;
                        }

                        var timeAgo = GetTimeAgo(request.CreatedAt);

                        result.Add(new
                        {
                            userId = requester.UserId,
                            name = fullName,
                            avatar = requester.Avatar ?? "/assets/user.png",
                            timeAgo = timeAgo
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending friend requests for user {UserId}", userId);
                return new List<object>();
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
            
            return dateTime.ToString("dd/MM/yyyy");
        }
    }
}

