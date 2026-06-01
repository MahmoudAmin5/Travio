using Ardalis.Specification;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.DTOs.DestinationDTO;

namespace Travio.Core.Domain.Specifications.Favorites;

public class GetFavDestinationSpec : Specification<UserFavorite, GetFavDestinationResponse>
{
    public GetFavDestinationSpec(int take, int skip, string userId)
    {
        Query.Where(uf => uf.UserId == userId)
            .OrderByDescending(uf => uf.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .Select(uf => new GetFavDestinationResponse
            {
                DestinationID = uf.DestinationId,
                Name = uf.Destination.Name,
                Description = uf.Destination.Description,
                Rating = uf.Destination.Rating,
                CityName = uf.Destination.City.Name,
                ImageUrls = uf.Destination.Images.Select(i => i.ImageURL).ToList()
            });

    }
}
