using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class NearbyDestinationsSpec : Specification<Destination>
{
    public NearbyDestinationsSpec(decimal latitude, decimal longitude, double radiusKm, int count)
    {
        // Approximate degree-based distance filter (1 degree ≈ 111 km)
        var degreeRadius = (decimal)(radiusKm / 111.0);

        Query.Where(x =>
            x.Latitude >= latitude - degreeRadius && x.Latitude <= latitude + degreeRadius &&
            x.Longitude >= longitude - degreeRadius && x.Longitude <= longitude + degreeRadius);

        Query.Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest)
                    .AsNoTracking();

        Query.OrderByDescending(x => x.Rating);
        Query.Take(count);
    }
}