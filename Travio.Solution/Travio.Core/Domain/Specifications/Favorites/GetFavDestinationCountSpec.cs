using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Favorites;

public class GetFavDestinationCountSpec : Specification<UserFavorite>
{
    public GetFavDestinationCountSpec(string userId)
    {
        Query.Where(uf => uf.UserId == userId)
             .AsNoTracking();
    }
}
