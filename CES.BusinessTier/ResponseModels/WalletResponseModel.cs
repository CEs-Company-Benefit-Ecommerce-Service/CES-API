using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class WalletResponseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Type { get; set; }
        public double? Balance { get; set; }
        public double? Limit { get; set; }
        public Guid? AccountId { get; set; }
    }
}
