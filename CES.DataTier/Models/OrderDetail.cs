﻿using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class OrderDetail
    {
        public Guid Id { get; set; }
        public double? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid ProductId { get; set; }
        public Guid? OrderId { get; set; }

        public virtual Order? Order { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}
