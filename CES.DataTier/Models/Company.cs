using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Company
    {
        public Company()
        {
            Accounts = new HashSet<Account>();
            DebtNotes = new HashSet<DebtNote>();
            Receipts = new HashSet<Receipt>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<DebtNote> DebtNotes { get; set; }
        public virtual ICollection<Receipt> Receipts { get; set; }
    }
}
