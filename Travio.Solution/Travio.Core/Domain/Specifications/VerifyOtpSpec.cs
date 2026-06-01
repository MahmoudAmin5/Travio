using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Enums;

namespace Travio.Core.Domain.Specifications
{
    public class VerifyOtpSpec : Specification<UserCode>
    {
        public VerifyOtpSpec(string UserId, string OtpCode, AuthCodeType type)
        {

            Query.Where(c =>
                c.ApplicationUserId == UserId &&
                !c.IsRevoked && !c.IsUsed &&
                c.CodeType == type &&
                c.Code == OtpCode &&
                c.ExpiryDate > DateTime.UtcNow)
             .OrderByDescending(c => c.CreatedOn)
             .Take(1);
        }
    }
}
