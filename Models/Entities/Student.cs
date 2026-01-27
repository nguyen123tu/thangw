using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTU.Models.Entities
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(20)]
        public string? MSSV { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? PlaceOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? Class { get; set; }

        [StringLength(100)]
        public string? Faculty { get; set; }

        [StringLength(20)]
        public string? Course { get; set; }

        public int TotalCredits { get; set; } = 0;

        [Column(TypeName = "decimal(3,2)")]
        public decimal GPA { get; set; } = 0.0m;

        public bool IsLinked { get; set; } = false;

        public DateTime? LinkedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
