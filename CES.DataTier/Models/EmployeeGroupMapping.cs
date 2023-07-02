using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class EmployeeGroupMapping
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Employee Employee { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
    }
}
