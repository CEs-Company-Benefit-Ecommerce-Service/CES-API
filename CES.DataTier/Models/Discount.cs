using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Discount
    {
        public int Id { get; set; }
        public int? Type { get; set; }
        public double? Amount { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? ProductId { get; set; }

        public virtual Product? Product { get; set; }
    }
}
