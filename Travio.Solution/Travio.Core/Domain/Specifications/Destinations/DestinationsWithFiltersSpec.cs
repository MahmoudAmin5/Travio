using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Enums;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationsWithFiltersSpec : Specification<Destination>
{
    public DestinationsWithFiltersSpec(int? cityId, int? interestId, int skip, int take, DestinationSortBy sortBy = DestinationSortBy.Newest)
    {
        Query.Where(x => (!cityId.HasValue || x.CityID == cityId) &&
                         (!interestId.HasValue || x.DestinationInterests.Any(di => di.InterestID == interestId)));

        Query.Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest)
                    .AsNoTracking();

        _ = sortBy switch
        {
            DestinationSortBy.Rating => Query.OrderByDescending(x => x.Rating),
            DestinationSortBy.RatingAsc => Query.OrderBy(x => x.Rating),
            DestinationSortBy.Name => Query.OrderBy(x => x.Name),
            DestinationSortBy.NameDesc => Query.OrderByDescending(x => x.Name),
            DestinationSortBy.Reviews => Query.OrderByDescending(x => x.TotalReviews),
            _ => Query.OrderByDescending(x => x.DestinationID)
        };

        Query.Skip(skip).Take(take);
    }
}
