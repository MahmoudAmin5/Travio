using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationByInterestIdSpec : Specification<Destination>
{
    public DestinationByInterestIdSpec(int interestId)
    {

        Query.Where(x => x.DestinationInterests.Any(di => di.InterestID == interestId))
            .Include(x => x.City)
            .Include(x => x.Images)
            .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest)
            .AsNoTracking();
    }
}