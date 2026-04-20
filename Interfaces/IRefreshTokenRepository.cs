using WebApplication1.Entities;
using WebApplication1.DTOs;

namespace WebApplication1.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
    }
}
