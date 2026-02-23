using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Destinations;

public class DestinationSearchSpec : Specification<Destination>
{
    public DestinationSearchSpec(string keyword, int skip, int take)
    {
        Query.Where(x => x.Name.Contains(keyword) || x.Description.Contains(keyword));

        Query.Include(x => x.City)
             .Include(x => x.Images)
             .Include(x => x.DestinationInterests)
                .ThenInclude(di => di.Interest)
                    .AsNoTracking();

        Query.OrderByDescending(x => x.Rating);
        Query.Skip(skip).Take(take);
    }
}