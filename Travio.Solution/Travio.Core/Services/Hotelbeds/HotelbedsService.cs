using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Travio.Core.Contracts.Services.CurruncyExchange;
using Travio.Core.Contracts.Services.GeocodingService;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Hotels;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.HotelbedsDTOs.Requests;
using Travio.Core.DTOs.HotelbedsDTOs.Responses;
using Travio.Core.Helpers;
using Travio.Core.Services.Hotelbeds.ApiModels;
using Travio.Core.Setting;

namespace Travio.Core.Services.Hotelbeds
{
    public partial class HotelbedsService : IHotelbedsService
    {
        private readonly HttpClient _httpClient;
        private readonly IGenericRepository<HotelBooking> _bookingRepository;
        private readonly IGenericRepository<HotelDestination> _destinationRepository;
        private readonly IGenericRepository<HotelFacility> _facilityRepository;
        private readonly IMemoryCache _cache;
        private readonly ICurrencyExchangeService _currencyExchangeService;
        private readonly ILogger<HotelbedsService> _logger;
        private readonly IGeocodingService _geocodingService;
        private readonly HotelbedsSettings _settings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly TimeSpan ContentCacheDuration = TimeSpan.FromHours(24);

        public HotelbedsService(
            HttpClient httpClient,
            IGenericRepository<HotelBooking> bookingRepository,
            IGenericRepository<HotelDestination> destinationRepository,
            IGenericRepository<HotelFacility> facilityRepository,
            IMemoryCache cache,
            IOptions<HotelbedsSettings> settings,
            ICurrencyExchangeService currencyExchangeService,
            ILogger<HotelbedsService> logger,
            IGeocodingService geocodingService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _destinationRepository = destinationRepository ?? throw new ArgumentNullException(nameof(destinationRepository));
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _currencyExchangeService = currencyExchangeService;
            _logger = logger;
            _geocodingService = geocodingService;
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 0: AUTOCOMPLETE SEARCH DESTINATIONS (Local DB)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<List<HotelDestination>>> SearchDestinationsAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new ServiceResponse<List<HotelDestination>>(new List<HotelDestination>(), "Query is empty.");

                var destinations = await _destinationRepository.ListAsync(cancellationToken);
                var results = destinations
                    .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                d.Code.Equals(query, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToList();

                return new ServiceResponse<List<HotelDestination>>(results, "Success");
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<HotelDestination>>($"Error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 1: SEARCH AVAILABILITY (with images from Content API)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<HotelAvailabilityResponseDto>> SearchAvailabilityAsync(
            HotelAvailabilityRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request is null)
                    return new ServiceResponse<HotelAvailabilityResponseDto>("Availability request cannot be null.");
                if (string.IsNullOrWhiteSpace(request.CheckIn) || string.IsNullOrWhiteSpace(request.CheckOut))
                    return new ServiceResponse<HotelAvailabilityResponseDto>("Check-in and check-out dates are required.");
                if (request.Occupancies is null || request.Occupancies.Count == 0)
                    return new ServiceResponse<HotelAvailabilityResponseDto>("At least one room occupancy is required.");
                if (string.IsNullOrWhiteSpace(request.DestinationCode) && (request.HotelCodes is null || request.HotelCodes.Count == 0))
                    return new ServiceResponse<HotelAvailabilityResponseDto>("Either a destination code or hotel codes must be provided.");
                if (!string.IsNullOrWhiteSpace(request.DestinationName))
                {
                    //var spec = new DestinationByNameSpec(request.DestinationName);

                    //// 2. Query the database using the Repository
                    //var destinationMatch = await _destinationRepository.FirstOrDefaultAsync(spec);

                    //if (destinationMatch != null && !string.IsNullOrEmpty(destinationMatch.Code))
                    //{
                    //    request.DestinationCode = destinationMatch.Code; // Success! We have the code.
                    //}

                    //// 2. THE FALLBACK: Local DB failed. Call an external geocoder.
                    //_logger.LogWarning("Destination '{Name}' not in local DB. Falling back to Geocoder.", request.DestinationName);

                    var coordinates = await _geocodingService.GetCoordinatesAsync(request.DestinationName);

                    if (coordinates != null)
                    {
                        request.Latitude = coordinates.Value.Lat;
                        request.Longitude = coordinates.Value.Lng;
                        request.RadiusInKm = 20; // Search a 20km circle around this point
                    }
                    else
                    {
                        return new ServiceResponse<HotelAvailabilityResponseDto>("We couldn't locate this destination on the map.");
                    }

                }
                //var distcode = _destinationRepository.FirstOrDefaultAsync();
                var apiRequest = BuildAvailabilityRequest(request);
                var httpResponse = await _httpClient.PostAsJsonAsync("hotels", apiRequest, JsonOptions, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(httpResponse, cancellationToken);
                    return new ServiceResponse<HotelAvailabilityResponseDto>($"Hotelbeds availability search failed: {err}");
                }

                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsAvailabilityResponse>(JsonOptions, cancellationToken);
                if (apiResponse?.Hotels?.Hotels is null || apiResponse.Hotels.Hotels.Count == 0)
                    return new ServiceResponse<HotelAvailabilityResponseDto>(
                        new HotelAvailabilityResponseDto { Hotels = new(), TotalHotels = 0 },
                        "No hotels found matching your search criteria.");
                var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync("EUR", "USD", cancellationToken);
                var responseDto = MapAvailabilityResponse(apiResponse, exchangeRate);


                // Enrich with images from Content API
                var hotelCodes = responseDto.Hotels.Select(h => h.Code).ToList();
                var contentMap = await GetHotelContentBatchAsync(hotelCodes, cancellationToken);
                foreach (var hotel in responseDto.Hotels)
                {
                    if (contentMap.TryGetValue(hotel.Code, out var content))
                    {
                        var images = MapThumbnailImages(content.Images);
                        hotel.Images = images.Take(5).ToList();
                        hotel.ThumbnailImage = images.FirstOrDefault()?.Url;
                    }
                }

                return new ServiceResponse<HotelAvailabilityResponseDto>(responseDto, $"Found {responseDto.TotalHotels} available hotels.");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<HotelAvailabilityResponseDto>($"Network error while searching hotels: {ex.Message}"); }
            catch (TaskCanceledException)
            { return new ServiceResponse<HotelAvailabilityResponseDto>("The hotel availability search request timed out."); }
            catch (Exception ex)
            { return new ServiceResponse<HotelAvailabilityResponseDto>($"An unexpected error occurred: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 2: HOTEL DETAILS (Content API + optional Availability)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<HotelDetailResponseDto>> GetHotelDetailsAsync(
            int hotelCode, HotelDetailsQueryDto? query, CancellationToken cancellationToken = default)
        {
            try
            {
                if (hotelCode <= 0)
                    return new ServiceResponse<HotelDetailResponseDto>("A valid hotel code is required.");


                var contentMap = await GetHotelContentBatchAsync(new List<int> { hotelCode }, cancellationToken);
                if (!contentMap.TryGetValue(hotelCode, out var content))
                    return new ServiceResponse<HotelDetailResponseDto>("Hotel not found in Hotelbeds.");

                string rawCategoryCode = content.CategoryCode ?? string.Empty;

                // 2. Instantly resolve it using your new static helper!
                string resolvedCategoryName = HotelbedsCategoryMapper.GetCategoryName(rawCategoryCode);
                var dto = new HotelDetailResponseDto
                {
                    Code = content.Code,
                    Name = content.Name?.Content ?? string.Empty,
                    Description = content.Description?.Content ?? string.Empty,
                    CategoryCode = rawCategoryCode,
                    CategoryName = resolvedCategoryName,
                    AccommodationType = HotelbedsAccommodationMapper.GetName(
        content.AccommodationType?.Code,
        content.AccommodationType?.TypeDescription),
                    Address = content.Address?.Content ?? string.Empty,
                    PostalCode = content.PostalCode ?? string.Empty,
                    City = content.City?.Content ?? string.Empty,
                    CountryCode = content.CountryCode ?? string.Empty,
                    DestinationCode = content.DestinationCode ?? string.Empty,
                    Latitude = content.Coordinates?.Latitude,
                    Longitude = content.Coordinates?.Longitude,
                    Email = content.Email,
                    Web = content.Web,
                    Phones = content.Phones?.Select(p => new HotelPhoneDto
                    {
                        Type = p.PhoneType ?? string.Empty,
                        Number = p.PhoneNumber ?? string.Empty
                    }).ToList() ?? new(),
                    Images = MapGalleryImages(content.Images),
                    Facilities = content.Facilities?.Select(f => new HotelFacilityDto
                    {
                        Code = f.FacilityCode,
                        GroupCode = f.FacilityGroupCode,
                        Description = f.Description?.Content ?? string.Empty
                    }).ToList() ?? new()
                };

                // 3. If dates provided, get live availability
                if (!string.IsNullOrWhiteSpace(query?.CheckIn) && !string.IsNullOrWhiteSpace(query?.CheckOut))
                {
                    var availRequest = new HotelAvailabilityRequestDto
                    {
                        CheckIn = query.CheckIn,
                        CheckOut = query.CheckOut,
                        HotelCodes = new List<int> { hotelCode },
                        Occupancies = new List<OccupancyDto>
                        {
                            new()
                            {
                                Adults = query.Adults > 0 ? query.Adults : 2,
                                Children = query.Children,
                                ChildrenAges = !string.IsNullOrWhiteSpace(query.ChildrenAges)
                                    ? query.ChildrenAges.Split(',').Select(a => int.TryParse(a.Trim(), out var age) ? age : 0).ToList()
                                    : null
                            }
                        }
                    };
                    var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync("EUR", "USD", cancellationToken);
                    var apiRequest = BuildAvailabilityRequest(availRequest);
                    var httpResponse = await _httpClient.PostAsJsonAsync("hotels", apiRequest, JsonOptions, cancellationToken);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var availResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsAvailabilityResponse>(JsonOptions, cancellationToken);
                        var hotelAvail = availResponse?.Hotels?.Hotels?.FirstOrDefault();
                        if (hotelAvail is not null)
                        {
                            // Load facility lookup for room-level facility resolution
                            var facilityLookup = await GetFacilityLookupAsync(cancellationToken);
                            dto.Rooms = MapRooms(hotelAvail.Rooms, content.Rooms, content.Images, exchangeRate, facilityLookup);
                            dto.MinRate = decimal.TryParse(hotelAvail.MinRate, out var min) ? Math.Round(min * 1.15m * exchangeRate, 2) : null;
                            dto.MaxRate = decimal.TryParse(hotelAvail.MaxRate, out var max) ? Math.Round(max * 1.15m * exchangeRate, 2) : null;
                            dto.Currency = "USD";
                        }
                    }
                }

                return new ServiceResponse<HotelDetailResponseDto>(dto, "Hotel details retrieved successfully.");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<HotelDetailResponseDto>($"Network error: {ex.Message}"); }
            catch (TaskCanceledException)
            { return new ServiceResponse<HotelDetailResponseDto>("The request timed out."); }
            catch (Exception ex)
            { return new ServiceResponse<HotelDetailResponseDto>($"An unexpected error occurred: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 3: CHECK RATE
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<HotelCheckRateResponseDto>> CheckRateAsync(
            HotelCheckRateRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request is null)
                    return new ServiceResponse<HotelCheckRateResponseDto>("CheckRate request cannot be null.");
                if (string.IsNullOrWhiteSpace(request.RateKey))
                    return new ServiceResponse<HotelCheckRateResponseDto>("Rate key is required.");

                var apiRequest = new HotelbedsCheckRateRequest
                {
                    Rooms = new List<HotelbedsCheckRateRoom> { new() { RateKey = request.RateKey } }
                };
                var httpResponse = await _httpClient.PostAsJsonAsync("checkrates", apiRequest, JsonOptions, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(httpResponse, cancellationToken);
                    return new ServiceResponse<HotelCheckRateResponseDto>($"Hotelbeds CheckRate failed: {err}");
                }

                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsCheckRateResponse>(JsonOptions, cancellationToken);
                if (apiResponse?.Hotel is null)
                    return new ServiceResponse<HotelCheckRateResponseDto>("The rate is no longer available.");

                var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync("EUR", "USD", cancellationToken);
                return new ServiceResponse<HotelCheckRateResponseDto>(MapCheckRateResponse(apiResponse, exchangeRate), "Rate confirmed. Proceed to booking.");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<HotelCheckRateResponseDto>($"Network error: {ex.Message}"); }
            catch (TaskCanceledException)
            { return new ServiceResponse<HotelCheckRateResponseDto>("The request timed out."); }
            catch (Exception ex)
            { return new ServiceResponse<HotelCheckRateResponseDto>($"Unexpected error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 4: CREATE BOOKING
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<HotelBookingResponseDto>> CreateBookingAsync(
            HotelBookingRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync("EUR", "USD", cancellationToken);
                if (request is null) return new ServiceResponse<HotelBookingResponseDto>("Booking request cannot be null.");
                if (string.IsNullOrWhiteSpace(request.RateKey)) return new ServiceResponse<HotelBookingResponseDto>("Rate key is required.");
                if (string.IsNullOrWhiteSpace(request.HolderFirstName) || string.IsNullOrWhiteSpace(request.HolderLastName))
                    return new ServiceResponse<HotelBookingResponseDto>("Holder's first and last name are required.");
                if (request.Rooms is null || request.Rooms.Count == 0) return new ServiceResponse<HotelBookingResponseDto>("At least one room is required.");
                if (string.IsNullOrWhiteSpace(userId)) return new ServiceResponse<HotelBookingResponseDto>("User must be authenticated.");

                var clientRef = string.IsNullOrWhiteSpace(request.ClientReference)
                    ? $"TRV-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}" : request.ClientReference;
                var apiRequest = BuildBookingRequest(request, clientRef);
                var httpResponse = await _httpClient.PostAsJsonAsync("bookings", apiRequest, JsonOptions, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(httpResponse, cancellationToken);
                    await PersistBookingRecordAsync(userId, request, null, HotelBookingStatus.PaymentFailed, exchangeRate, cancellationToken);
                    return new ServiceResponse<HotelBookingResponseDto>($"Hotelbeds booking failed: {err}");
                }

                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsBookingResponse>(JsonOptions, cancellationToken);
                if (apiResponse?.Booking is null)
                {
                    await PersistBookingRecordAsync(userId, request, null, HotelBookingStatus.PaymentFailed, exchangeRate, cancellationToken);
                    return new ServiceResponse<HotelBookingResponseDto>("Booking response was empty.");
                }

                await PersistBookingRecordAsync(userId, request, apiResponse.Booking, HotelBookingStatus.Confirmed, exchangeRate, cancellationToken);

                var responseDto = MapBookingResponse(apiResponse, exchangeRate);
                return new ServiceResponse<HotelBookingResponseDto>(responseDto, $"Hotel booked! Reference: {responseDto.BookingReference}");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<HotelBookingResponseDto>($"Network error: {ex.Message}"); }
            catch (TaskCanceledException)
            { return new ServiceResponse<HotelBookingResponseDto>("The booking request timed out."); }
            catch (Exception ex)
            { return new ServiceResponse<HotelBookingResponseDto>($"Unexpected error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE: Facility Lookup Cache (from local DB)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Loads the facility lookup table from the database and caches it in memory for 24h.
        /// Returns a dictionary mapping FacilityCode → human-readable description.
        /// </summary>
        private async Task<IReadOnlyDictionary<int, string>> GetFacilityLookupAsync(CancellationToken ct)
        {
            const string cacheKey = "hotelbeds_facility_lookup";
            if (_cache.TryGetValue<IReadOnlyDictionary<int, string>>(cacheKey, out var cached) && cached is not null)
                return cached;

            try
            {
                var facilities = await _facilityRepository.ListAsync(ct);
                var lookup = facilities
                    .Where(f => !string.IsNullOrWhiteSpace(f.Description))
                    .GroupBy(f => f.FacilityCode)
                    .ToDictionary(g => g.Key, g => g.First().Description);

                _cache.Set(cacheKey, (IReadOnlyDictionary<int, string>)lookup, ContentCacheDuration);
                return lookup;
            }
            catch
            {
                // If DB query fails, return empty — room facilities just won't appear
                return new Dictionary<int, string>();
            }
        }
    }
}
