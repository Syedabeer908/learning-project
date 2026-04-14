using System.ComponentModel.DataAnnotations;
using WebApplication1.DTOs;

namespace WebApplication1.DTOs.Admin
{
    public class AdminUserDto : BaseUserDto
    {
        public required Guid RoleId { get; set; }

        public required string RoleName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
