using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Core.Domain.Specifications.Favorites;

public class SpecificUserFavoriteSpec : Specification<UserFavorite>, ISingleResultSpecification<UserFavorite>
{
    public SpecificUserFavoriteSpec(string userId, int destinationId)
    {
        Query.Where(uf => uf.UserId == userId && uf.DestinationId == destinationId);
    }
}
