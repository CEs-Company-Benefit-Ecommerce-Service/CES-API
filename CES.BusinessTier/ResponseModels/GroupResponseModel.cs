using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class GroupResponseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? BenefitId { get; set; }

        public BenefitResponseModel Benefit { get; set; } = null!;
        public List<EmployeeGroupMapping> EmployeeGroupMappings { get; set; }
    }

    public partial class GroupAccountResponse
    {
        public Guid Id { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? AccountId { get; set; }

        public AccountResponseModel? Account { get; set; }
        public GroupResponseModel? Group { get; set; }
    }
}
