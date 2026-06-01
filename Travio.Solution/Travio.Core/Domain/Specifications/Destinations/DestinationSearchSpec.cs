using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationSearchSpec : Specification<Destination>
{
    public DestinationSearchSpec(string keyword, int skip, int take, List<int>? interestIds = null)
    {
        var lowerKeyword = keyword.ToLower();

        Query.Where(x =>
            x.Name.ToLower().Contains(lowerKeyword) ||
            x.Description.ToLower().Contains(lowerKeyword) ||
            x.City.Name.ToLower().Contains(lowerKeyword) ||
            x.City.Country.Name.ToLower().Contains(lowerKeyword) ||
            x.City.Country.Continent.Name.ToLower().Contains(lowerKeyword) ||
            x.DestinationInterests.Any(di =>
                di.Interest.InterestName.ToLower().Contains(lowerKeyword))
        );

        if (interestIds is { Count: > 0 })
        {
            Query.Where(x => x.DestinationInterests.Any(di => interestIds.Contains(di.InterestID)));
        }

        Query.Include(x => x.City)
                .ThenInclude(c => c.Country)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest);

        Query.AsNoTracking();
        Query.OrderByDescending(x => x.Rating);
        Query.Skip(skip).Take(take);
    }
}