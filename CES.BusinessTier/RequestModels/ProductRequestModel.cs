using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class ProductRequestModel
    {
        public double? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ServiceDuration { get; set; }
        public int? Type { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
    }
}
