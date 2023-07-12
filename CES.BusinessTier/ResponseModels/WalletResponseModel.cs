using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class WalletResponseModel
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public double? Balance { get; set; }
        public double? Used { get; set; }
        public double? Limits { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

    }
}
