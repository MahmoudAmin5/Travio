using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class CountriesWithDestinationsSpec : Specification<Country>
{
    public CountriesWithDestinationsSpec()
    {
        Query.Where(c => c.Cities.Any(city => city.Destinations.Any()))
             .AsNoTracking();
    }
}
