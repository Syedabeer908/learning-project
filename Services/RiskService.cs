using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.Data;
using WebApplication1.DTOs.Risk;
using WebApplication1.Entities;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class RiskService
    {
        private readonly IRepository<Risk> _repo;
        private readonly ISoftRepository _softRepo;

        public RiskService(IRepository<Risk> repo, ISoftRepository softRepo)
        {
            _repo = repo;
            _softRepo = softRepo;
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

        private Risk ToEntity(Guid userId, CreateRiskDto dto)
        {
            return new Risk
            {
                RiskId = Guid.NewGuid(),
                UserId = userId,
                RiskTitle = dto.RiskTitle,
                RiskDescription = dto.RiskDescription,
                CreatedBy = userId
            };
        }

        private void UpdateEntity(Risk risk, Guid userId, UpdateRiskDto dto)
        {
            risk.RiskTitle = dto.RiskTitle;
            risk.RiskDescription = dto.RiskDescription;
            risk.LastUpdatedAt = DateTime.UtcNow;
            risk.LastUpdatedBy = userId;
        }

        private void PatchEntity(Risk risk, Guid userId, PatchRiskDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RiskTitle))
                risk.RiskTitle = dto.RiskTitle;
            if (!string.IsNullOrEmpty(dto.RiskDescription))
                risk.RiskDescription = dto.RiskDescription;
            risk.LastUpdatedAt = DateTime.UtcNow;
            risk.LastUpdatedBy = userId;
        }

        private async Task<Risk> CheckRiskExistAndGet(Guid id)
        {
            var risk = await _repo.GetByIdAsync(id);

            if (risk == null)
                throw new NotFoundException($"Risk with id {id} not found.");

            return risk;
        }

        private async Task RunSoftService(Guid id, bool action, Guid actionBy)
        {
            var soft = new Soft();
            var values = soft.SoftValuesSetter(action, DateTime.UtcNow, actionBy);
            await _softRepo.SoftRiskAsync(id, values);
        }

        public async Task<ResultT<List<RiskDto>>> GetAllAsync()
        {
            var risks = await _repo.GetAllAsync();
            var riskDtos = risks.Select(r => ToDto(r)).ToList();
            return ResultT<List<RiskDto>>.Success(riskDtos);
        }

        public async Task<ResultT<RiskDto>> GetByIdAsync(Guid id)
        {
            var risk = await CheckRiskExistAndGet(id);
            return ResultT<RiskDto>.Success(ToDto(risk));
        }

        public async Task<ResultT<RiskDto>> AddAsync(Guid userId, CreateRiskDto dto)
        {
            var risk = ToEntity(userId, dto);
            await _repo.AddAsync(risk);

            var data = await CheckRiskExistAndGet(risk.RiskId);

            return ResultT<RiskDto>.Success(ToDto(data));
        }

        public async Task<ResultT<RiskDto>> UpdateAsync(Guid id, Guid userId, UpdateRiskDto dto)
        {
            var risk = await CheckRiskExistAndGet(id);
            UpdateEntity(risk, userId, dto);
            await _repo.UpdateAsync(risk);
            return ResultT<RiskDto>.Success(ToDto(risk));
        }

        public async Task<Result> DeleteAsync(Guid id, Guid userId)
        {
            var risk = await CheckRiskExistAndGet(id);
            await RunSoftService(id, true, userId);
            return Result.Success();
        }

        public async Task<Result> RestoreAsync(Guid id, Guid userId)
        {
            var risk = await CheckRiskExistAndGet(id);
            await RunSoftService(id, false, userId);
            return Result.Success();
        }

        public async Task<ResultT<RiskDto>> PatchAsync(Guid id, Guid userId, PatchRiskDto dto)
        {
            var risk = await CheckRiskExistAndGet(id);
            PatchEntity(risk, userId, dto);
            await _repo.UpdateAsync(risk);
            return ResultT<RiskDto>.Success(ToDto(risk));
        }
    }
}
