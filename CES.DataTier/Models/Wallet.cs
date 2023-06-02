using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Wallet
    {
        public Wallet()
        {
            Transactions = new HashSet<Transaction>();
            WalletTransactions = new HashSet<WalletTransaction>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Type { get; set; }
        public double? Balance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? AccountId { get; set; }

        public virtual Account? Account { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
