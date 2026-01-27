using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("Friendships")]
    public class Friendship
    {
        [Key]
        public int FriendshipId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int FriendId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
