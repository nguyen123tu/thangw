using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly MTUSocialDbContext _context;

        public StudentRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetByUserIdAsync(int userId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsLinked);
        }

        public async Task<Student> UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student> CreateForUserAsync(int userId)
        {
            var student = new Student
            {
                UserId = userId,
                MSSV = $"MTU{userId:D6}",
                IsLinked = true,
                LinkedAt = DateTime.Now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }
    }
}

