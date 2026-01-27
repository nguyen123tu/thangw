using MTU.Models.DTOs;

namespace MTU.Services
{
    public interface IFriendshipService
    {
        Task<List<FriendDto>> GetFriendsAsync(int userId);
        Task<List<FriendDto>> GetFriendSuggestionsAsync(int currentUserId);
        Task<List<FriendRequestDto>> GetPendingRequestsAsync(int userId);
        Task<FriendshipResult> SendFriendRequestAsync(int userId, int friendId);
        Task<FriendshipResult> AcceptFriendRequestAsync(int userId, int friendId);
        Task<FriendshipResult> DeclineFriendRequestAsync(int userId, int friendId);
        Task<int> GetFriendCountAsync(int userId);
        Task<List<object>> GetPendingFriendRequestsAsync(int userId);
    }
}

