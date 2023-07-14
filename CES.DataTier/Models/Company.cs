using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Company
    {
        public Company()
        {
            BankTransactions = new HashSet<BankTransaction>();
            Benefits = new HashSet<Benefit>();
            DebtTickets = new HashSet<DebtTicket>();
            Employees = new HashSet<Employee>();
            Enterprises = new HashSet<Enterprise>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ContactPersonId { get; set; }
        public double? Limits { get; set; }
        public double? Used { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? TimeOut { get; set; }

        public virtual ICollection<BankTransaction> BankTransactions { get; set; }
        public virtual ICollection<Benefit> Benefits { get; set; }
        public virtual ICollection<DebtTicket> DebtTickets { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
        public virtual ICollection<Enterprise> Enterprises { get; set; }
    }
}
