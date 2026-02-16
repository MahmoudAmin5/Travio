using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationByCityIdSpec : Specification<Destination>
{
    public DestinationByCityIdSpec(int Cityid)
    {
        Query.Where(x => x.CityID == Cityid)
             .Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                 .ThenInclude(di => di.Interest).AsNoTracking();
    }
}
