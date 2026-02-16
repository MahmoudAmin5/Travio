using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.Destination;

public interface IDestinationService
{
    Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? interestId);
    Task<DestinationDto> GetByIdAsync(int id);
    Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count);
    Task<IEnumerable<DestinationDto>> GetByCityAsync(int cityId);
    Task<IEnumerable<DestinationDto>> GetByInterestAsync(int interestId);
}
