using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class ProductResponseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CategoryId { get; set; }
        public int? DiscountId { get; set; }
        public Guid? SupplierId { get; set; }

        public virtual Category Category { get; set; } = null!;
        public virtual Discount? Discount { get; set; }
        public virtual Supplier? Supplier { get; set; }

    }
}
