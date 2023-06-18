using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class WalletRequestModel
    {
        public string? Name { get; set; }
        public int? Type { get; set; }
        public double? Balance { get; set; }
        public Guid? AccountId { get; set; }

        //public WalletTransaction WalletTransaction { get; set; }
    }
    public class WalletInfoRequestModel
    {
        public string? Name { get; set; }
        public int? Type { get; set; }
        public Guid? AccountId { get; set; }
        public double? Limit { get; set; }
    }
}
