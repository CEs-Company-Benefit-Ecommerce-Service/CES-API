using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class GroupRequestModel
    {
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public Guid? BenefitId { get; set; }
    }
    public class GroupMemberRequestModel
    {
        public Guid GroupId { get; set; }
        public List<Guid> AccountId { get; set; }
    }
}
