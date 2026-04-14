using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;
using WebApplication1.Interfaces;

namespace WebApplication1.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
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

        public async Task<List<User>> GetAllAsync(string? search = "",
            bool? isActive = null, Guid? lastId = null, int page = 1, int pageSize = 10)
        {
            var query = _context.User.AsQueryable();

            //if(lastId != null)
            //{
            //    query = query.Where(u => u.UserId > lastId);
            //}

            if (isActive != null) 
            {
                query = query.Where(u => u.IsActive == isActive);
            }

            if(!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.Role.Name.ToLower().Contains(search));
            }

            return await query
                .Include(u => u.Role)
                .OrderBy(b => b.Id)
                .Take(pageSize)
                .ToListAsync();
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

        public async Task<int> CountAsync(string roleName)
        {
            return await _context.User
                .Where(u => u.Role.Name.ToLower() == roleName.ToLower())
                .CountAsync();
        }
    }
}
