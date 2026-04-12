using Mapster;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Enums;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Destinations;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Helpers;

namespace Travio.Core.Services.Destinations;

public class DestinationService : IDestinationService
{
    private readonly IGenericRepository<Destination> _destinationRepository;
    private readonly IGenericRepository<Country> _countryRepository;

    public DestinationService(
        IGenericRepository<Destination> destinationRepository,
        IGenericRepository<Country> countryRepository)
    {
        _destinationRepository = destinationRepository;
        _countryRepository = countryRepository;
    }

    public async Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? countryId, int? interestId, DestinationSortBy sortBy = DestinationSortBy.Newest)
    {
        var skip = (pageIndex - 1) * pageSize;
        var dataSpec = new DestinationsWithFiltersSpec(cityId, interestId, countryId, skip, pageSize, sortBy);
        var countSpec = new DestinationFilterSpec(cityId, interestId, countryId);
        var totalItems = await _destinationRepository.CountAsync(countSpec);
        var data = await _destinationRepository.ListAsync(dataSpec);
        var dataDto = data.Adapt<IEnumerable<DestinationDto>>();
        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto.ToList());
    }

    public async Task<DestinationDto?> GetByIdAsync(int id)
    {
        var spec = new DestinationByIdSpec(id);
        var destination = await _destinationRepository.FirstOrDefaultAsync(spec);
        return destination?.Adapt<DestinationDto>();
    }

    public async Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count = 10)
    {
        var spec = new TopRatedDestinationsSpec(count);
        var destinations = await _destinationRepository.ListAsync(spec);
        return destinations.Adapt<IEnumerable<DestinationDto>>();
    }

    public async Task<Pagination<DestinationDto>> SearchByNameAsync(string keyword, int pageIndex, int pageSize)
    {
        var skip = (pageIndex - 1) * pageSize;
        var dataSpec = new DestinationSearchSpec(keyword, skip, pageSize);
        var countSpec = new DestinationSearchCountSpec(keyword);
        var totalItems = await _destinationRepository.CountAsync(countSpec);
        var data = await _destinationRepository.ListAsync(dataSpec);
        var dataDto = data.Adapt<IEnumerable<DestinationDto>>();
        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto.ToList());
    }

    public async Task<IEnumerable<DestinationDto>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, int count = 10)
    {
        var spec = new NearbyDestinationsSpec(latitude, longitude, radiusKm, count);
        var destinations = await _destinationRepository.ListAsync(spec);
        return destinations.Adapt<IEnumerable<DestinationDto>>();
    }

    public async Task<IEnumerable<CountryDto>> GetFamousCountriesAsync()
    {
        var spec = new CountriesWithDestinationsSpec();
        var countries = await _countryRepository.ListAsync(spec);
        return countries.Adapt<IEnumerable<CountryDto>>();
    }
}
