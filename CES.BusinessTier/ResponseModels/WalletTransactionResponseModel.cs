using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class WalletTransactionResponseModel
    {
        public Guid? SenderId { get; set; }
        public Guid? RecieverId { get; set; }
        public double? Total { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? Status { get; set; }
        public int? Type { get; set; }

        public Guid? WalletId { get; set; }
    }
}
