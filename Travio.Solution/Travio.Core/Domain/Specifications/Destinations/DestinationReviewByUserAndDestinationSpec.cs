using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationReviewByUserAndDestinationSpec : Specification<DestinationReview>, ISingleResultSpecification<DestinationReview>
{
    public DestinationReviewByUserAndDestinationSpec(int destinationId, string userId, bool activeOnly = true)
    {
        Query.Where(x =>
            x.DestinationId == destinationId &&
            x.UserId == userId &&
            (!activeOnly || x.IsActive));
    }
}
