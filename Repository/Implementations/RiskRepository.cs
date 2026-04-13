using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Repository.Implementations
{
    public class RiskRepository : IRepository<Risk>
    {
        private readonly ApplicationDbContext _context;

        public RiskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Risk>> GetAllAsync() => await _context.Risk.ToListAsync();

        public async Task<Risk?> GetByIdAsync(Guid riskId) =>
            await _context.Risk.FirstOrDefaultAsync(r => r.RiskId == riskId);

        public async Task AddAsync(Risk risk)
        {
            await _context.Risk.AddAsync(risk);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Risk risk)
        {
            _context.Risk.Update(risk);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Risk risk)
        {
            _context.Risk.Remove(risk);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckIfExist(int id)
        {
            return await _context.Risk.AnyAsync(r => r.Id == id);
        }
    }
}
