using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.Admin
{
    public class PatchAdminUserDto : BasePatchUserDto
    {
        public Guid? RoleId { get; set; }

    }
}
