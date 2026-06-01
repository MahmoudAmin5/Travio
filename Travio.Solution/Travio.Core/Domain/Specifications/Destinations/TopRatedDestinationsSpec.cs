using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class TopRatedDestinationsSpec : Specification<Destination>
{
    public TopRatedDestinationsSpec(int count)
    {
        Query.OrderByDescending(x => x.Rating)
             .Take(count)
             .Include(x => x.City)
             .Include(x => x.Images);
    }
}