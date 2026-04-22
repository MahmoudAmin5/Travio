using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class ActiveDestinationReviewStatsSpec : Specification<DestinationReview>
{
    public ActiveDestinationReviewStatsSpec(int destinationId)
    {
        Query.Where(x => x.DestinationId == destinationId && x.IsActive)
            .AsNoTracking();
    }
}
