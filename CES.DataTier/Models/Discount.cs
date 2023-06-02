﻿using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Discount
    {
        public int Id { get; set; }
        public int? Type { get; set; }
        public double? Amount { get; set; }
        public string? ImageUrl { get; set; }
        public string? ExpirationDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? ProductId { get; set; }

        public virtual Product? Product { get; set; }
    }
}
