using WebApplication1.Common.Exceptions;
using WebApplication1.Entities;
using WebApplication1.DTOs.Control;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class ControlService
    {
        private readonly IRepository<Control> _repo;

        public ControlService(IRepository<Control> repo)
        {
            _repo = repo;
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

        private Control ToEntity(CreateControlDto dto)
        {
            return new Control
            {
                ControlId = Guid.NewGuid(),
                ControlTitle = dto.ControlTitle,
                ControlDescription = dto.ControlDescription
            };
        }

        private void UpdateEntity(Control control, UpdateControlDto dto)
        {
            control.ControlTitle = dto.ControlTitle;
            control.ControlDescription = dto.ControlDescription;
        }

        private void PatchEntity(Control control, PatchControlDto dto)
        {
            if (!string.IsNullOrEmpty(dto.ControlTitle))
                control.ControlTitle = dto.ControlTitle;
            if (!string.IsNullOrEmpty(dto.ControlDescription))
                control.ControlDescription = dto.ControlDescription;
        }

        private async Task<Control> CheckControlExistAndGet(Guid id)
        {
            var control = await _repo.GetByIdAsync(id);

            if (control == null)
                throw new NotFoundException($"Risk with id {id} not found.");

            return control;
        }

        public async Task<List<ControlDto>> GetAllAsync()
        {
            var controls = await _repo.GetAllAsync();
            return controls.Select(c => ToDto(c)).ToList();
        }

        public async Task<ControlDto> GetByIdAsync(Guid id)
        {
            var control = await CheckControlExistAndGet(id);
            return ToDto(control);
        }

        public async Task<ControlDto> AddAsync(CreateControlDto dto)
        {
            var control = ToEntity(dto);
            await _repo.AddAsync(control);
            return ToDto(control);
        }

        public async Task<ControlDto> UpdateAsync(Guid id, UpdateControlDto dto)
        {
            var control = await CheckControlExistAndGet(id);
            UpdateEntity(control, dto);
            await _repo.UpdateAsync(control);
            return ToDto(control);
        }

        public async Task DeleteAsync(Guid id)
        {
            var control = await CheckControlExistAndGet(id);
            await _repo.DeleteAsync(control);
        }

        public async Task<ControlDto> PatchAsync(Guid id, PatchControlDto dto)
        {
            var control = await CheckControlExistAndGet(id);
            PatchEntity(control, dto);
            await _repo.UpdateAsync(control);
            return ToDto(control);
        }
    }
}
