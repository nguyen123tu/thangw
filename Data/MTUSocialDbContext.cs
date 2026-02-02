using Microsoft.EntityFrameworkCore;
using MTU.Models.Entities;

namespace MTU.Data
{
    public class MTUSocialDbContext : DbContext
    {
        public MTUSocialDbContext(DbContextOptions<MTUSocialDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.HasOne(u => u.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.PostId);
                
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId);
                
                entity.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasKey(e => e.LikeId);
                
                entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();
                
                entity.HasOne(l => l.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(l => l.User)
                    .WithMany(u => u.Likes)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<PostTag>(entity =>
            {
                entity.HasKey(e => e.PostTagId);
                
                entity.HasOne(pt => pt.Post)
                    .WithMany(p => p.PostTags)
                    .HasForeignKey(pt => pt.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(pt => pt.Tag)
                    .WithMany(t => t.PostTags)
                    .HasForeignKey(pt => pt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                entity.HasIndex(e => e.MSSV).IsUnique();
            });
            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasKey(e => e.FriendshipId);
                entity.HasIndex(e => new { e.UserId, e.FriendId }).IsUnique();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageId);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagId);
                entity.HasIndex(e => e.TagName).IsUnique();
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);
            });

            modelBuilder.Entity<Story>(entity =>
            {
                entity.HasKey(e => e.StoryId);
                
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SavedPost>(entity =>
            {
                entity.HasKey(e => e.SavedPostId);
                
                entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique();
                
                entity.HasOne(s => s.Post)
                    .WithMany(p => p.SavedPosts)
                    .HasForeignKey(s => s.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(s => s.User)
                    .WithMany(u => u.SavedPosts)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
