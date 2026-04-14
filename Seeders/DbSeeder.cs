using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApplication1.Configuration;
using WebApplication1.Entities;

namespace WebApplication1.Seeders
{
    public class DbSeeder
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _hasher;
        private readonly RoleConfig _roleConfig;
        public DbSeeder(AppDbContext context, IOptions<RoleConfig> roleOptions)
        {
            _context = context;
            _hasher = new PasswordHasher<User>();
            _roleConfig= roleOptions.Value;
        }
        public async Task SeedAsync()
        {
            if (!await _context.Role.AnyAsync())
            {
                _context.Role.AddRange(
                    new Role { RoleId = Guid.NewGuid(), Name = _roleConfig.Admin },
                    new Role { RoleId = Guid.NewGuid(), Name = _roleConfig.User }
                );

                await _context.SaveChangesAsync();
            }

            var adminRole = await _context.Role
                .FirstOrDefaultAsync(r => r.Name == _roleConfig.Admin);

            if (adminRole == null)
                return;

            bool adminExists = await _context.User
                .AnyAsync(u => u.RoleId == adminRole.RoleId);

            if (adminExists)
                return;

            var adminUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@transviti.com",
                RoleId = adminRole.RoleId,
                IsActive = true
            };

            adminUser.PasswordHash = _hasher.HashPassword(adminUser, "Abeer@l1");

            _context.User.Add(adminUser);
            await _context.SaveChangesAsync();
           
        }
    }
}
