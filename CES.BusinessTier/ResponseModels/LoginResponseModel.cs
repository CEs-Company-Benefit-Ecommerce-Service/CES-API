using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class LoginResponseModel
    {
        public AccountResponseModel Account { get; set; }
        public TokenModel Token { get; set; }
    }
}
