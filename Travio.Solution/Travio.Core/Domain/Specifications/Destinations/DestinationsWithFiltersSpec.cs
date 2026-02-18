using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationsWithFiltersSpec : Specification<Destination>
{
    public DestinationsWithFiltersSpec(int? cityId, int? interestId, int skip, int take)
    {

        Query.Where(x => (!cityId.HasValue || x.CityID == cityId) &&
                         (!interestId.HasValue || x.DestinationInterests.Any(di => di.InterestID == interestId)));


        Query.Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest)
                    .AsNoTracking();



        Query.OrderByDescending(x => x.DestinationID);


        Query.Skip(skip).Take(take);
    }
}
