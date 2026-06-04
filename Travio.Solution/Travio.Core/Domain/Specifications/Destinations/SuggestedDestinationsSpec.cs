using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

/// <summary>
/// Finds destinations similar to a given destination based on shared interests,
/// same country, same continent — ordered by relevance then rating.
/// </summary>
public class SuggestedDestinationsSpec : Specification<Destination>
{
    public SuggestedDestinationsSpec(
        int currentDestinationId,
        List<int> interestIds,
        int countryId,
        int continentId,
        int count)
    {
        // Exclude the current destination
        Query.Where(x => x.DestinationID != currentDestinationId);

        // Must share at least one interest with the current destination
        Query.Where(x => x.DestinationInterests.Any(di => interestIds.Contains(di.InterestID)));

        Query.Include(x => x.City)
                .ThenInclude(c => c.Country)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest);

        Query.AsNoTracking();

        // Order: same country first, then same continent, then by shared interest count desc, then rating desc
        Query.OrderByDescending(x => x.City.CountryID == countryId ? 1 : 0)
             .ThenByDescending(x => x.City.Country.ContinentID == continentId ? 1 : 0)
             .ThenByDescending(x => x.DestinationInterests.Count(di => interestIds.Contains(di.InterestID)))
             .ThenByDescending(x => x.Rating);

        Query.Take(count);
    }
}
