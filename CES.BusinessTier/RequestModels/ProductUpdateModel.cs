using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class ProductUpdateModel
    {
        public string? Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public double? UnitPrice { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public int? Status { get; set; }
    }
}
