using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Entities
{
    public class User : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        [Required]
        [Column(TypeName = "nvarchar(200)")]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public int TokenVersion { get; set; } = 0;

        public ICollection<Risk> Risks { get; set; } = new List<Risk>();
        public ICollection<Control> Controls { get; set; } = new List<Control>();
        public ICollection<RiskControl> RiskControls { get; set; } = new List<RiskControl>();
    }
}
