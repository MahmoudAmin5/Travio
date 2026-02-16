using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationFilterSpec : Specification<Destination>
{
    public DestinationFilterSpec(int? cityId, int? interestId)
    {
        Query.Where(x => (!cityId.HasValue || x.CityID == cityId) &&
                         (!interestId.HasValue || x.DestinationInterests.Any(di => di.InterestID == interestId))).AsNoTracking();
    }
}