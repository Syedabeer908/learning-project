using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;

namespace WebApplication1.Repository.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.User.Include(u => u.Role)
                                       .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.User.Include(u => u.Role)
                                       .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByRoleIdAsync(Guid roleId)
        {
            return await _context.User.Include(u => u.Role)
                                       .FirstOrDefaultAsync(u => u.RoleId == roleId);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.User.Include(u => u.Role).ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.User.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.User.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.User.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
        {
            return await _context.User
                .AnyAsync(u => u.Email.ToLower() == email.ToLower()
                               && (!excludeId.HasValue || u.UserId != excludeId));
        }

        public async Task<int> CountAsync(string role)
        {
            return await _context.User.CountAsync(u => u.Role.Name.Equals(role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
