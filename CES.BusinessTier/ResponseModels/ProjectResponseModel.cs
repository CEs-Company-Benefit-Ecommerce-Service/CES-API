using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class ProjectResponseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public int? CompanyId { get; set; }

        public ICollection<ProjectAccountResponse> ProjectAccounts { get; set; }
    }

    public partial class ProjectAccountResponse
    {
        public Guid Id { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? AccountId { get; set; }

        public AccountResponseModel? Account { get; set; }
        public ProjectResponseModel? Project { get; set; }
    }
}
