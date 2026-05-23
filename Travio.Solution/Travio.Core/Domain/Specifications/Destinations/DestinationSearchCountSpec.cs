using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationSearchCountSpec : Specification<Destination>
{
    public DestinationSearchCountSpec(string keyword)
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
    }
}