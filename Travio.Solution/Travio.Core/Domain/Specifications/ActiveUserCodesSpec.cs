using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Enums;

namespace Travio.Core.Domain.Specifications
{
    public class ActiveUserCodesSpec : Specification<UserCode>
    {
        public ActiveUserCodesSpec(string UserId , AuthCodeType type)
        {
            Query.Where(u => u.ApplicationUserId == UserId && !u.IsRevoked && u.IsUsed && u.CodeType == type);
        }
    }
}
