using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Group
    {
        public Group()
        {
            Accounts = new HashSet<Account>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParentGroupId { get; set; }
        public int Status { get; set; }
        public int CompanyId { get; set; }

        public virtual Company Company { get; set; } = null!;
        public virtual ICollection<Account> Accounts { get; set; }
    }
}
