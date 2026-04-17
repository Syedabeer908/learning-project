using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Entities.Enums;
using WebApplication1.DTOs.RiskControl;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class RiskControlService
    {
        private readonly IRepository<RiskControl> _repo;
        private readonly IRepository<Risk> _riskRepo;
        private readonly IRepository<Control> _controlRepo;
        private readonly ISoftRepository _softRepo;

        public RiskControlService(IRepository<RiskControl> repo, IRepository<Risk> riskRepo,
                                  IRepository<Control> controlRepo, ISoftRepository softRepo)
        {
            _repo = repo;
            _riskRepo = riskRepo;
            _controlRepo = controlRepo;
            _softRepo =  softRepo;
        }

        private RiskControlDto ToDto(RiskControl riskControl)
        {
            return new RiskControlDto
            {
                RiskControlId = riskControl.RiskControlId,
                RiskTitle = riskControl.Risk.RiskTitle,
                ControlTitle = riskControl.Control.ControlTitle,
                ControlMethod = ControlMethodToString(riskControl.ControlMethod)
            };
        }

        private async Task<RiskControl> ToEntity(Guid userId, CreateRiskControlDto dto)
        {
            var risk = await _riskRepo.GetByIdAsync(dto.RiskId);
            if (risk == null) throw new NotFoundException($"Risk with GUID {dto.RiskId} not found.");

            var control = await _controlRepo.GetByIdAsync(dto.ControlId);
            if (control == null) throw new NotFoundException($"Control with GUID {dto.ControlId} not found.");


            return new RiskControl
            {
                RiskControlId = Guid.NewGuid(),
                UserId = userId,
                RiskId = risk.RiskId,
                ControlId = control.ControlId,
                ControlMethod = ParseControlMethod(dto.ControlMethod),
                CreatedBy = userId
            };
        }

        private void UpdateEntity(RiskControl riskControl, Guid userId, UpdateRiskControlDto dto)
        {
            riskControl.ControlMethod = ParseControlMethod(dto.ControlMethod);
            riskControl.LastUpdatedAt = DateTime.UtcNow;
            riskControl.LastUpdatedBy = userId;
        }

        private async Task PatchEntity(RiskControl riskControl, Guid userId, PatchRiskControlDto dto)
        {
            if (!string.IsNullOrEmpty(dto.ControlMethod))
                riskControl.ControlMethod = ParseControlMethod(dto.ControlMethod);
            riskControl.LastUpdatedAt = DateTime.UtcNow;
            riskControl.LastUpdatedBy = userId;
        }

        private ControlMethod ParseControlMethod(string method)
        {
            if (!Enum.TryParse<ControlMethod>(method, true, out var result))
                throw new NotFoundException($"Invalid ControlMethod: {method}");
            return result;
        }

        private string ControlMethodToString(ControlMethod method)
        {
            return method.ToString(); 
        }

        private async Task<RiskControl> CheckRiskControlExistAndGet(Guid id)
        {
            var riskControl = await _repo.GetByIdAsync(id);

            if (riskControl == null)
                throw new NotFoundException($"RiskControl with id {id} not found.");

            return riskControl;
        }

        private async Task RunSoftService(Guid id, bool action, Guid actionBy)
        {
            var soft = new Soft();
            var values = soft.SoftValuesSetter(action, DateTime.UtcNow, actionBy);
            await _softRepo.SoftRiskControlAsync(id, values);
        }

        public async Task<ResultT<List<RiskControlDto>>> GetAllAsync()
        {
            var riskControls = await _repo.GetAllAsync();
            var riskControlsDtos = riskControls.Select(rc => ToDto(rc)).ToList();
            return ResultT<List<RiskControlDto>>.Success(riskControlsDtos);
        }

        public async Task<ResultT<RiskControlDto>> GetByIdAsync(Guid id)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);
            return ResultT<RiskControlDto>.Success(ToDto(riskControl));
        }

        public async Task<ResultT<RiskControlDto>> AddAsync(Guid userId, CreateRiskControlDto dto)
        {
            var riskControl = await ToEntity(userId, dto);
            await _repo.AddAsync(riskControl);

            var data = await CheckRiskControlExistAndGet(riskControl.RiskControlId);

            return ResultT<RiskControlDto>.Success(ToDto(data));
        }

        public async Task<ResultT<RiskControlDto>> UpdateAsync(Guid id, Guid userId, UpdateRiskControlDto dto)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);

            UpdateEntity(riskControl, userId, dto);
            await _repo.UpdateAsync(riskControl);
            return ResultT<RiskControlDto>.Success(ToDto(riskControl));
        }

        public async Task<Result> DeleteAsync(Guid id, Guid userId)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);
            await RunSoftService(id, true, userId);
            return Result.Success();
        }

        public async Task<Result> RestoreAsync(Guid id, Guid userId)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);
            await RunSoftService(id, false, userId);
            return Result.Success();
        }

        public async Task<ResultT<RiskControlDto>> PatchAsync(Guid id, Guid userId, PatchRiskControlDto dto)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);

            await PatchEntity(riskControl, userId, dto);
            await _repo.UpdateAsync(riskControl);
            return ResultT<RiskControlDto>.Success(ToDto(riskControl));
        }
    }
}