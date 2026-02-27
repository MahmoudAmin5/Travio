using Travio.Core.Domain.Enums;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.Destination;

public interface IDestinationService
{
    Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? interestId, DestinationSortBy sortBy = DestinationSortBy.Newest);
    Task<DestinationDto?> GetByIdAsync(int id);
    Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count = 10);
    Task<Pagination<DestinationDto>> SearchByNameAsync(string keyword, int pageIndex, int pageSize);
    Task<IEnumerable<DestinationDto>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, int count = 10);
    Task<IEnumerable<CountryDto>> GetFamousCountriesAsync();
}
