using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationSearchCountSpec : Specification<Destination>
{
    public DestinationSearchCountSpec(string keyword)
    {
        Query.Where(x => x.Name.Contains(keyword) || x.Description.Contains(keyword));
    }
}