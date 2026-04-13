using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.Admin
{
    public class AdminUserDto : BaseUserDto
    {
        public required Guid RoleId { get; set; }

        public required string RoleName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
