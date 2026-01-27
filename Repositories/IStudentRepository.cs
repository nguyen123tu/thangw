using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface IStudentRepository
    {
        Task<Student?> GetByUserIdAsync(int userId);
        Task<Student> UpdateAsync(Student student);
        Task<Student> CreateForUserAsync(int userId);
    }
}

