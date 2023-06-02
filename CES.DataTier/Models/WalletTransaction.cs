﻿using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class WalletTransaction
    {
        public Guid Id { get; set; }
        public double? Total { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? Status { get; set; }
        public int? Type { get; set; }
        public Guid? SenderId { get; set; }
        public Guid? RecieverId { get; set; }
        public Guid? WalletId { get; set; }

        public virtual Wallet? Wallet { get; set; }
    }
}
