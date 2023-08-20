using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Group
    {
        public Group()
        {
            EmployeeGroupMappings = new HashSet<EmployeeGroupMapping>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? BenefitId { get; set; }
        public int? Type { get; set; }
        public DateTime? TimeFilter { get; set; }
        public int? DateFilter { get; set; }
        public int? DayFilter { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? FirstTime { get; set; }

        public virtual Benefit? Benefit { get; set; }
        public virtual ICollection<EmployeeGroupMapping> EmployeeGroupMappings { get; set; }
    }
}
