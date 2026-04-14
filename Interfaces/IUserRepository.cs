using WebApplication1.Entities;

namespace WebApplication1.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRoleIdAsync(Guid roleId);
        Task<List<User>> GetAllAsync(string? search = "",
            bool? isActive = null, Guid? lastId = null, int page = 1, int pageSize = 10);

        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);

        Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
        Task<int> CountAsync(string role);

    }
}
