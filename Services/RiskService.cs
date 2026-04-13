using WebApplication1.Repository.Interfaces;
using WebApplication1.Entities;
using WebApplication1.DTOs.Risk;
using WebApplication1.Exceptions;

namespace WebApplication1.Services
{
    public class RiskService
    {
        private readonly IRepository<Risk> _repo;

        public RiskService(IRepository<Risk> repo)
        {
            _repo = repo;
        }

        private RiskDto ToDto(Risk risk)
        {
            return new RiskDto
            {
                RiskId = risk.RiskId,
                RiskTitle = risk.RiskTitle,
                RiskDescription = risk.RiskDescription
            };
        }

        private Risk ToEntity(CreateRiskDto dto)
        {
            return new Risk
            {
                RiskId = Guid.NewGuid(),
                RiskTitle = dto.RiskTitle,
                RiskDescription = dto.RiskDescription
            };
        }

        private void UpdateEntity(Risk risk, UpdateRiskDto dto)
        {
            risk.RiskTitle = dto.RiskTitle;
            risk.RiskDescription = dto.RiskDescription;
        }

        private void PatchEntity(Risk risk, PatchRiskDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RiskTitle))
                risk.RiskTitle = dto.RiskTitle;
            if (!string.IsNullOrEmpty(dto.RiskDescription))
                risk.RiskDescription = dto.RiskDescription;
        }

        private async Task<Risk> CheckRiskExistAndGet(Guid id)
        {
            var risk = await _repo.GetByIdAsync(id);

            if (risk == null)
                throw new NotFoundException($"Risk with id {id} not found.");

            return risk;
        }

        public async Task<List<RiskDto>> GetAllAsync()
        {
            var risks = await _repo.GetAllAsync();
            return risks.Select(r => ToDto(r)).ToList();
        }

        public async Task<RiskDto> GetByIdAsync(Guid id)
        {
            var risk = await CheckRiskExistAndGet(id);
            return ToDto(risk);
        }

        public async Task<RiskDto> AddAsync(CreateRiskDto dto)
        {
            var risk = ToEntity(dto);
            await _repo.AddAsync(risk);
            return ToDto(risk);
        }

        public async Task<RiskDto> UpdateAsync(Guid id, UpdateRiskDto dto)
        {
            var risk = await CheckRiskExistAndGet(id);
            UpdateEntity(risk, dto);
            await _repo.UpdateAsync(risk);
            return ToDto(risk);
        }

        public async Task DeleteAsync(Guid id)
        {
            var risk = await CheckRiskExistAndGet(id);
            await _repo.DeleteAsync(risk);
        }

        public async Task<RiskDto> PatchAsync(Guid id, PatchRiskDto dto)
        {
            var risk = await CheckRiskExistAndGet(id);
            PatchEntity(risk, dto);
            await _repo.UpdateAsync(risk);
            return ToDto(risk);
        }
    }
}
