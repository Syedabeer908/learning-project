using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
