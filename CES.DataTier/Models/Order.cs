﻿using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }

        public Guid Id { get; set; }
        public double Total { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? DebtStatus { get; set; }
        public Guid EmployeeId { get; set; }
        public int? DebtId { get; set; }

        public virtual Employee Employee { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
