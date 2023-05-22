using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Company
    {
        public Company()
        {
            Groups = new HashSet<Group>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }

        public virtual ICollection<Group> Groups { get; set; }
    }
}
