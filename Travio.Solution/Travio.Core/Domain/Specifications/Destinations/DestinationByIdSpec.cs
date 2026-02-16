using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationByIdSpec : Specification<Destination>
{
    public DestinationByIdSpec(int id)
    {
        Query.Where(x => x.DestinationID == id)
             .Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                 .ThenInclude(di => di.Interest).AsNoTracking();
    }
}
