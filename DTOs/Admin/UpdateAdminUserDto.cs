using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.Admin
{
    public class UpdateAdminUserDto : BaseUpdateUserDto
    {
        [Required]
        public required Guid RoleId { get; set; }

    }
}
