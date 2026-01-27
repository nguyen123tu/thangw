using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface IFriendshipRepository
    {        Task<List<Friendship>> GetFriendsAsync(int userId);        Task<List<Friendship>> GetPendingRequestsAsync(int userId);        Task<Friendship?> GetFriendshipAsync(int userId, int friendId);        Task<Friendship> CreateAsync(Friendship friendship);        Task<Friendship> UpdateAsync(Friendship friendship);        Task DeleteAsync(Friendship friendship);        Task<bool> AreFriendsAsync(int userId, int friendId);        Task<bool> HasPendingRequestAsync(int userId, int friendId);        Task<List<int>> GetFriendIdsAsync(int userId);
    }
}

