using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Favorites;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.Helpers;

namespace Travio.Core.Services.Destinations;

public class UserFavoriteService : IUserFavoriteService
{

    private readonly IGenericRepository<UserFavorite> _favRepo;

    public UserFavoriteService(IGenericRepository<UserFavorite> favRepo)
    {
        _favRepo = favRepo;
    }

    public async Task<ServiceResponse<bool>> AddFavoriteAsync(string userId, int destinationId)
    {
        var Spec = new SpecificUserFavoriteSpec(userId, destinationId);
        var existingFav = await _favRepo.FirstOrDefaultAsync(Spec);
        if (existingFav is not null)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "This destination is already in your favorites."
            };
        }
        var addFav = new UserFavorite
        {
            UserId = userId,
            DestinationId = destinationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await _favRepo.AddAsync(addFav);
        if (result is null)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to add favorite. Please try again."
            };
        }
        return new ServiceResponse<bool>
        {
            Success = true,
            Message = "Favorite added successfully."
        };
    }

    public async Task<ServiceResponse<bool>> DeleteFavoriteAsync(string userId, int destinationId)
    {
        var Spec = new SpecificUserFavoriteSpec(userId, destinationId);
        var existingFav = await _favRepo.FirstOrDefaultAsync(Spec);

        if (existingFav is null)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "This destination is not found in your favorites."
            };
        }

        await _favRepo.DeleteAsync(existingFav);
        return new ServiceResponse<bool>
        {
            Success = true,
            Message = "Favorite deleted successfully."
        };
    }

    public async Task<ServiceResponse<Pagination<GetFavDestinationResponse>>> GetUserFavoritesAsync(int pageIndex, int pageSize, string userId)
    {
        int skip = (pageIndex - 1) * pageSize;
        var spec = new GetFavDestinationSpec(pageSize, skip, userId);
        var countSpec = new GetFavDestinationCountSpec(userId);
        var result = await _favRepo.ListAsync(spec);
        var totalCount = await _favRepo.CountAsync(countSpec);
        var data = new Pagination<GetFavDestinationResponse>(pageIndex, pageSize, totalCount, result);
        return new ServiceResponse<Pagination<GetFavDestinationResponse>>
        {
            Success = true,
            Data = data,
            Message = "Favorites retrieved successfully."
        };
    }
}
