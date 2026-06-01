using Ardalis.Specification;
using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Specifications;

public class GetExpiredOtpsSpec : Specification<UserCode>
{
    public GetExpiredOtpsSpec()
    {
        Query.Where(u => u.ExpiryDate < DateTime.UtcNow);
    }
}
