using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

/// <summary>
/// Fallback spec: returns top-rated destinations in the same country,
/// used when the current destination has no interests assigned.
/// </summary>
public class SuggestedDestinationsFallbackSpec : Specification<Destination>
{
    public SuggestedDestinationsFallbackSpec(int currentDestinationId, int countryId, int count)
    {
        Query.Where(x => x.DestinationID != currentDestinationId);
        Query.Where(x => x.City.CountryID == countryId);

        Query.Include(x => x.City)
                .ThenInclude(c => c.Country)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest);

        Query.AsNoTracking();
        Query.OrderByDescending(x => x.Rating);
        Query.Take(count);
    }
}
