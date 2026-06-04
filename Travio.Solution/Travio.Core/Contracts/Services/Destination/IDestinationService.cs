using Travio.Core.Domain.Enums;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.Destination;

public interface IDestinationService
{
    Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? countryId, int? interestId, DestinationSortBy sortBy = DestinationSortBy.Newest);
    Task<DestinationDto?> GetByIdAsync(int id);
    Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count = 10);
    Task<Pagination<DestinationDto>> SearchByNameAsync(string keyword, int pageIndex, int pageSize, List<int>? interestIds = null);
    Task<IEnumerable<DestinationDto>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, int count = 10);
    Task<IEnumerable<CountryDto>> GetFamousCountriesAsync();
    Task<Pagination<DestinationReviewDto>> GetReviewsAsync(int destinationId, int pageIndex = 1, int pageSize = 10, string? currentUserId = null);
    Task<DestinationReviewMutationDto> UpsertMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto);
    Task<DestinationReviewMutationDto> UpdateMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto);
    Task<DestinationReviewDeleteResultDto> DeleteMyReviewAsync(int destinationId, string userId);
    Task<IEnumerable<DestinationDto>> GetSuggestedAsync(int destinationId, int count = 10);
}
