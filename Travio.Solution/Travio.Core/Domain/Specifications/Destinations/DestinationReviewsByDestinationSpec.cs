using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationReviewsByDestinationSpec : Specification<DestinationReview>
{
    public DestinationReviewsByDestinationSpec(int destinationId, int skip, int take)
    {
        Query.Where(x => x.DestinationId == destinationId && x.IsActive)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.ReviewId)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
