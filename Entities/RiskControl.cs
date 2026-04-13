using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Entities.Enums;

namespace WebApplication1.Entities
{
    public class RiskControl
    {
        

        [Key] 
        public int Id { get; set; }

        [Required]
        public  Guid RiskControlId { get; set; }

        [Required] 
        public  Guid RiskId { get; set; }

        public Risk Risk { get; set; } = null!;

        [Required]
        public  Guid ControlId { get; set; }

        public  Control Control { get; set; } = null!;

        [Required]
        public ControlMethod ControlMethod { get; set; }
    }
}
