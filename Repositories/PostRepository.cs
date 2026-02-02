using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly MTUSocialDbContext _context;

        public PostRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Post>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);
        }

        public async Task<Post> CreateAsync(Post post)
        {
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;
            post.IsDeleted = false;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return post;
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Posts
                .Where(p => !p.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetLikeCountAsync(int postId)
        {
            return await _context.Likes
                .Where(l => l.PostId == postId)
                .CountAsync();
        }

        public async Task<int> GetCommentCountAsync(int postId)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .CountAsync();
        }

        public async Task DeleteAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.IsDeleted = true;
                post.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Post>> GetFriendPostsAsync(int userId, int pageNumber, int pageSize)
        {
            // Lấy danh sách ID bạn bè
            var friendIds = await _context.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == "accepted")
                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                .ToListAsync();

            // Query:
            // 1. Bài của mình (UserId == userId) -> Lấy hết
            // 2. Bài của bạn bè (friendIds contains) -> Chỉ lấy Public hoặc Friends
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && (
                    p.UserId == userId || 
                    (friendIds.Contains(p.UserId) && (p.Privacy == "public" || p.Privacy == "friends"))
                ))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetExplorePostsAsync(int userId, int pageNumber, int pageSize)
        {
            // Lấy danh sách ID bạn bè
            var friendIds = await _context.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == "accepted")
                .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                .ToListAsync();

            friendIds.Add(userId); // Thêm bản thân để loại trừ

            // Khám phá: Bài người lạ -> Chỉ lấy Public
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && !friendIds.Contains(p.UserId) && p.Privacy == "public")
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> SearchAsync(string query, int limit)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Post>();

            var searchTerm = query.ToLower().Trim();

            return await _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && 
                            p.Privacy == "public" && 
                            p.Content.ToLower().Contains(searchTerm))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task UpdatePrivacyAsync(int postId, string privacy)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.Privacy = privacy;
                post.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}

