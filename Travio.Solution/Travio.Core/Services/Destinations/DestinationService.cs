using Mapster;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Destinations;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Helpers;

namespace Travio.Core.Services.Destinations;

public class DestinationService : IDestinationService
{
    private readonly IGenericRepository<Destination> _destinationRepository;

    public DestinationService(IGenericRepository<Destination> destinationRepository)
    {
        _destinationRepository = destinationRepository;
    }

    public async Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? interestId)
    {
        var skip = (pageIndex - 1) * pageSize;
        var dataSpec = new DestinationsWithFiltersSpec(cityId, interestId, skip, pageSize);
        var countSpec = new DestinationFilterSpec(cityId, interestId);
        var totalItems = await _destinationRepository.CountAsync(countSpec);
        var data = await _destinationRepository.ListAsync(dataSpec);
        var dataDto = data.Adapt<IEnumerable<DestinationDto>>();
        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto.ToList());

    }
    public async Task<DestinationDto> GetByIdAsync(int id)
    {
        var spec = new DestinationByIdSpec(id);
        var destination = await _destinationRepository.FirstOrDefaultAsync(spec);
        return destination.Adapt<DestinationDto>();
    }

    public async Task<IEnumerable<DestinationDto>> GetByCityAsync(int cityId)
    {
        var spec = new DestinationByCityIdSpec(cityId);
        var destinations = await _destinationRepository.ListAsync(spec);
        var dataDto = destinations.Adapt<IEnumerable<DestinationDto>>();
        return dataDto.ToList();
    }


    public async Task<IEnumerable<DestinationDto>> GetByInterestAsync(int interestId)
    {

        var spec = new DestinationByInterestIdSpec(interestId);
        var destinations = await _destinationRepository.ListAsync(spec);
        var dataDto = destinations.Adapt<IEnumerable<DestinationDto>>();
        return dataDto.ToList();
    }

    public async Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count)
    {
        var spec = new TopRatedDestinationsSpec(count);
        var destinations = await _destinationRepository.ListAsync(spec);
        return destinations.Adapt<IEnumerable<DestinationDto>>();
    }
}
