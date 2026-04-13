using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Entities
{
    public class Control
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid ControlId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(200)")]
        public string ControlTitle { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string ControlDescription { get; set; }

        public ICollection<RiskControl> RiskControl { get; set; } = new List<RiskControl>();
    }
}
