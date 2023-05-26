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
    }
}
