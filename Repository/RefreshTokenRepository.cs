using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WebApplication1.DTOs;
using WebApplication1.Entities;
using WebApplication1.Interfaces;

namespace WebApplication1.Repository
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshToken.AsNoTracking().FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }
    }
}
