using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.Entities;
using WebApplication1.DTOs.Control;
using WebApplication1.Interfaces;
using WebApplication1.Data.SoftDelete.Services;

namespace WebApplication1.Services
{
    public class ControlService
    {
        private readonly IRepository<Control> _repo;
        private readonly SoftDeleteService _softDeleteService;

        public ControlService(IRepository<Control> repo, SoftDeleteService softDeleteService)
        {
            _repo = repo;
            _softDeleteService = softDeleteService;
        }

        private ControlDto ToDto(Control control)
        {
            return new ControlDto
            {
                ControlId = control.ControlId,
                ControlTitle = control.ControlTitle,
                ControlDescription = control.ControlDescription
            };
        }

        private Control ToEntity(Guid userId, CreateControlDto dto)
        {
            return new Control
            {
                ControlId = Guid.NewGuid(),
                UserId = userId,
                ControlTitle = dto.ControlTitle,
                ControlDescription = dto.ControlDescription,
                CreatedBy = userId
            };
        }

        private void UpdateEntity(Control control, Guid userId, UpdateControlDto dto)
        {
            control.ControlTitle = dto.ControlTitle;
            control.ControlDescription = dto.ControlDescription;
            control.LastUpdatedAt = DateTime.UtcNow;
            control.LastUpdatedBy = userId;
        }

        private void PatchEntity(Control control, Guid userId, PatchControlDto dto)
        {
            if (!string.IsNullOrEmpty(dto.ControlTitle))
                control.ControlTitle = dto.ControlTitle;
            if (!string.IsNullOrEmpty(dto.ControlDescription))
                control.ControlDescription = dto.ControlDescription;
            control.LastUpdatedAt = DateTime.UtcNow;
            control.LastUpdatedBy = userId;
        }

        private async Task<Control> CheckControlExistAndGet(Guid id)
        {
            var control = await _repo.GetByIdAsync(id);

            if (control == null)
                throw new NotFoundException($"Control with id {id} not found.");

            return control;
        }

        public async Task<ResultT<List<ControlDto>>> GetAllAsync()
        {
            var controls = await _repo.GetAllAsync();
            var controlDtos = controls.Select(c => ToDto(c)).ToList();
            return ResultT<List<ControlDto>>.Success(controlDtos);
        }

        public async Task<ResultT<ControlDto>> GetByIdAsync(Guid id)
        {
            var control = await CheckControlExistAndGet(id);
            return ResultT<ControlDto>.Success(ToDto(control));
        }

        public async Task<ResultT<ControlDto>> AddAsync(Guid userId, CreateControlDto dto)
        {
            var control = ToEntity(userId, dto);
            await _repo.AddAsync(control);
            return ResultT<ControlDto>.Success(ToDto(control));
        }

        public async Task<ResultT<ControlDto>> UpdateAsync(Guid id, Guid userId, UpdateControlDto dto)
        {
            var control = await CheckControlExistAndGet(id);
            UpdateEntity(control, userId, dto);
            await _repo.UpdateAsync(control);
            return ResultT<ControlDto>.Success(ToDto(control));
        }

        public async Task<Result> DeleteAsync(Guid id, Guid userId)
        {
            var control = await CheckControlExistAndGet(id);
            await _softDeleteService.DeleteAsync<Control>(id, userId);
            return Result.Success();
        }

        public async Task<Result> RestoreAsync(Guid id, Guid userId)
        {
            var control = await CheckControlExistAndGet(id);
            await _softDeleteService.RestoreAsync<Control>(id, userId);
            return Result.Success();
        }

        public async Task<ResultT<ControlDto>> PatchAsync(Guid id, Guid userId, PatchControlDto dto)
        {
            var control = await CheckControlExistAndGet(id);
            PatchEntity(control, userId, dto);
            await _repo.UpdateAsync(control);
            return ResultT<ControlDto>.Success(ToDto(control));
        }
    }
}
