using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.DTOs.DestinationDTO;

namespace Travio.Core.Domain.Specifications.Destinations;

public class CountriesWithDestinationsSpec : Specification<Country, CountryDto>
{
    public CountriesWithDestinationsSpec()
    {
        Query.Where(c => c.Cities.Any(city => city.Destinations.Any()))
             .AsNoTracking();

        Query.Select(c => new CountryDto
        {
            CountryID = c.CountryID,
            Name = c.Name,
            ImageURL = c.ImageURL
        });
    }
}
