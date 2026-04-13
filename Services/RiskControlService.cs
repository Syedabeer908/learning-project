using WebApplication1.DTOs.RiskControl;
using WebApplication1.Entities;
using WebApplication1.Entities.Enums;   
using WebApplication1.Repository.Interfaces;
using WebApplication1.Exceptions;

namespace WebApplication1.Services
{
    public class RiskControlService
    {
        private readonly IRepository<RiskControl> _repo;
        private readonly IRepository<Risk> _riskRepo;
        private readonly IRepository<Control> _controlRepo;

        public RiskControlService(IRepository<RiskControl> repo, IRepository<Risk> riskRepo,
                                  IRepository<Control> controlRepo)
        {
            _repo = repo;
            _riskRepo = riskRepo;
            _controlRepo = controlRepo;
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

        private async Task<RiskControl> ToEntity(CreateRiskControlDto dto)
        {
            var risk = await _riskRepo.GetByIdAsync(dto.RiskId);
            if (risk == null) throw new NotFoundException($"Risk with GUID {dto.RiskId} not found.");

            var control = await _controlRepo.GetByIdAsync(dto.ControlId);
            if (control == null) throw new NotFoundException($"Control with GUID {dto.ControlId} not found.");


            return new RiskControl
            {
                RiskControlId = Guid.NewGuid(),
                RiskId = risk.RiskId,
                ControlId = control.ControlId,
                ControlMethod = ParseControlMethod(dto.ControlMethod)
            };
        }

        private void UpdateEntity(RiskControl riskControl, UpdateRiskControlDto dto)
        {
            riskControl.ControlMethod = ParseControlMethod(dto.ControlMethod);
;
        }

        private async Task PatchEntity(RiskControl riskControl, PatchRiskControlDto dto)
        {
            if (!string.IsNullOrEmpty(dto.ControlMethod))
                riskControl.ControlMethod = ParseControlMethod(dto.ControlMethod);
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

        public async Task<List<RiskControlDto>> GetAllAsync()
        {
            var riskControls = await _repo.GetAllAsync();
            return riskControls.Select(rc => ToDto(rc)).ToList();
        }

        public async Task<RiskControlDto> GetByIdAsync(Guid id)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);
            return ToDto(riskControl);
        }

        public async Task<RiskControlDto> AddAsync(CreateRiskControlDto dto)
        {
            var riskControl = await ToEntity(dto);
            await _repo.AddAsync(riskControl);
            return ToDto(riskControl);
        }

        public async Task<RiskControlDto> UpdateAsync(Guid id, UpdateRiskControlDto dto)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);

            UpdateEntity(riskControl, dto);
            await _repo.UpdateAsync(riskControl);
            return ToDto(riskControl);
        }

        public async Task DeleteAsync(Guid id)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);
            await _repo.DeleteAsync(riskControl);
        }

        public async Task<RiskControlDto> PatchAsync(Guid id, PatchRiskControlDto dto)
        {
            var riskControl = await CheckRiskControlExistAndGet(id);

            await PatchEntity(riskControl, dto);
            await _repo.UpdateAsync(riskControl);
            return ToDto(riskControl);
        }
    }
}