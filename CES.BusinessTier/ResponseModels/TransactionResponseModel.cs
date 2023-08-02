using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class TransactionResponseModel
    {
        public Guid? Id { get; set; }
        public double? Total { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? SenderId { get; set; }
        public Guid? RecieveId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? WalletId { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public Guid? PaymentProviderId { get; set; }
        public string? InvoiceId { get; set; }
        public int? Status { get; set; } // 0 = đang thanh toán || 1 = complete || 2 = false
    }
}
