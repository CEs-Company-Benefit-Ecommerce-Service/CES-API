using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class ProjectRequestModel
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
    }
    public class ProjectMemberRequestModel
    {
        public Guid ProjectId { get; set; }
        public List<Guid> AccountId { get; set; }
    }
}
