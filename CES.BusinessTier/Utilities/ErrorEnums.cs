using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Utilities
{
    public enum AccountErrorEnums
    {
        [Display(Name = "Not found this account id!")]
        NOT_FOUND_ID = 4041,
        [Display(Name = "Not found this account!")]
        NOT_FOUND = 4001,
        [Display(Name = "Account do not have permission!")]
        NOT_HAVE_PERMISSION = 4031,
    }

    public enum CompanyErrorEnums
    {
        [Display(Name = "Invalid company id!")]
        INVALID_COMPANY_ID = 4031,
    }

    public enum CategoryErrorEnums
    {
        [Display(Name = "Invalid category!")]
        INVALID_CATEGORY = 4001,
        [Display(Name = "Not found this category!")]
        NOT_FOUND_CATEGORY = 4041,
    }
}
