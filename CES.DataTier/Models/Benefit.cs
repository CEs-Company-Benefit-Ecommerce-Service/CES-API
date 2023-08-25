using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Benefit
    {
        public Benefit()
        {
            Groups = new HashSet<Group>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public double UnitPrice { get; set; }
        public int? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CompanyId { get; set; }
        public double? TotalReceive { get; set; }
        public double? EstimateTotal { get; set; }

        public virtual Company Company { get; set; } = null!;
        public virtual ICollection<Group> Groups { get; set; }
    }
}
