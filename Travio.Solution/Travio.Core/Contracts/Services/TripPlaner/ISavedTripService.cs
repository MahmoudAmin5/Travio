using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.TripPlaner;

public interface ISavedTripService
{
    Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserTripsAsync(int pageIndex, int pageSize, string userId);
    Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserFavoriteTripsAsync(int pageIndex, int pageSize, string userId);
    Task<ServiceResponse<SavedTripDetailDto>> GetTripByIdAsync(int tripId, string userId);
    Task<ServiceResponse<bool>> ToggleFavoriteAsync(int tripId, string userId);
    Task<ServiceResponse<bool>> DeleteTripAsync(int tripId, string userId);
}
