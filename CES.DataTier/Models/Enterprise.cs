using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Enterprise
    {
        public Guid Id { get; set; }
        public int CompanyId { get; set; }
        public Guid AccountId { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Company Company { get; set; } = null!;
    }
}
