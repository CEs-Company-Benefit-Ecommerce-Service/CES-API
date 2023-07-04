using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Wallet
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public double? Balance { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? AccountId { get; set; }

        public virtual Account? Account { get; set; }
    }
}
