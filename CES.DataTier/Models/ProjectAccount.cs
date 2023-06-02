using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class ProjectAccount
    {
        public Guid Id { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? AccountId { get; set; }

        public virtual Account? Account { get; set; }
        public virtual Project? Project { get; set; }
    }
}
