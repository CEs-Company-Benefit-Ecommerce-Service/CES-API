using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Role
    {
        public Role()
        {
            Accounts = new HashSet<Account>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime? UpdateAt { get; set; }
        public Guid? UpdateBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public int Status { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
    }
}
