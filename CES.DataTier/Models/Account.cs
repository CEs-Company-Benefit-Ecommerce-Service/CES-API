using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Account
    {
        public Account()
        {
            Transactions = new HashSet<Transaction>();
            Wallets = new HashSet<Wallet>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime? UpdateAt { get; set; }
        public Guid? UpdateBy { get; set; }
        public DateTime? CreateAt { get; set; }
        public string? ImageUrl { get; set; }
        public int Status { get; set; }
        public int RoleId { get; set; }
        public Guid GroupId { get; set; }

        public virtual Group Group { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<Wallet> Wallets { get; set; }
    }
}
