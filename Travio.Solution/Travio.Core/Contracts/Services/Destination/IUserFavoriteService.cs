using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.Destination;

public interface IUserFavoriteService
{

    Task<ServiceResponse<bool>> AddFavoriteAsync(string userId, int destinationId);

    Task<ServiceResponse<bool>> DeleteFavoriteAsync(string userId, int destinationId);

    Task<ServiceResponse<Pagination<GetFavDestinationResponse>>> GetUserFavoritesAsync(int pageIndex, int pageSize, string userId);
}
