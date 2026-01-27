using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly MTUSocialDbContext _context;

        public FriendshipRepository(MTUSocialDbContext context)
        {
            _context = context;
        }        public async Task<List<Friendship>> GetFriendsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) 
                            && f.Status == "accepted")
                .ToListAsync();
        }        public async Task<List<Friendship>> GetPendingRequestsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => f.FriendId == userId && f.Status == "pending")
                .ToListAsync();
        }        public async Task<Friendship?> GetFriendshipAsync(int userId, int friendId)
        {
            return await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId));
        }        public async Task<Friendship> CreateAsync(Friendship friendship)
        {
            friendship.CreatedAt = DateTime.Now;
            friendship.UpdatedAt = DateTime.Now;
            
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            
            return friendship;
        }        public async Task<Friendship> UpdateAsync(Friendship friendship)
        {
            friendship.UpdatedAt = DateTime.Now;
            
            _context.Friendships.Update(friendship);
            await _context.SaveChangesAsync();
            
            return friendship;
        }        public async Task DeleteAsync(Friendship friendship)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }        public async Task<bool> AreFriendsAsync(int userId, int friendId)
        {
            return await _context.Friendships
                .AnyAsync(f => 
                    ((f.UserId == userId && f.FriendId == friendId) ||
                     (f.UserId == friendId && f.FriendId == userId))
                    && f.Status == "accepted");
        }        public async Task<bool> HasPendingRequestAsync(int userId, int friendId)
        {
            return await _context.Friendships
                .AnyAsync(f => 
                    ((f.UserId == userId && f.FriendId == friendId) ||
                     (f.UserId == friendId && f.FriendId == userId))
                    && f.Status == "pending");
        }        public async Task<List<int>> GetFriendIdsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) 
                            && f.Status == "accepted")
                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                .ToListAsync();
        }
    }
}

