using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly MTUSocialDbContext _context;

        public LikeRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<Like?> GetAsync(int postId, int userId)
        {
            return await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<Like> CreateAsync(Like like)
        {
            like.CreatedAt = DateTime.Now;

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return like;
        }

        public async Task DeleteAsync(Like like)
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int postId, int userId)
        {
            return await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }
    }
}

