using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;

namespace WebApplication1.Repository.Implementations
{
    public class RiskControlRepository : IRepository<RiskControl>
    {
        private readonly ApplicationDbContext _context;

        public RiskControlRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RiskControl>> GetAllAsync() => 
          await _context.RiskControl
                .Include(rc => rc.Risk)
                .Include(rc => rc.Control)
                .ToListAsync();

        public async Task<RiskControl?> GetByIdAsync(Guid riskControlId) =>
            await _context.RiskControl
                .Include(rc => rc.Risk)
                .Include(rc => rc.Control)
                .FirstOrDefaultAsync(rc => rc.RiskControlId == riskControlId);

        public async Task AddAsync(RiskControl riskControl)
        {
            await _context.RiskControl.AddAsync(riskControl);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RiskControl riskControl)
        {
            _context.RiskControl.Update(riskControl);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(RiskControl riskControl)
        {
            _context.RiskControl.Remove(riskControl);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckIfExist(int id)
        {
            return await _context.RiskControl.AnyAsync(r => r.Id == id);
        }
    }
}
