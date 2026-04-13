using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.Admin
{
    public class CreateAdminUserDto : AuthRegisterDto
    {
        [Required]
        public required Guid RoleId { get; set; }
    }
}
