using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.HotelbedsDTOs.Requests;
using Travio.Core.DTOs.HotelbedsDTOs.Responses;
using Travio.Core.Services.Hotelbeds.ApiModels;

namespace Travio.Core.Services.Hotelbeds
{
    public partial class HotelbedsService
    {
        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 5: GET USER BOOKINGS (from Database)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<List<UserHotelBookingDto>>> GetUserBookingsAsync(
            string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return new ServiceResponse<List<UserHotelBookingDto>>("User must be authenticated.");

                // Rule 6: Direct projection to DTO via .Select(...) backed by Covering Index IX_HotelBookings_UserId_CreatedAt
                // Eliminates Clustered Index Key Lookups, Sort operator, and GuestDataJson LOB reads!
                var userBookings = await _bookingRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new UserHotelBookingDto
                    {
                        Id = b.Id,
                        HotelId = b.HotelId,
                        HotelName = b.HotelName,
                        CheckIn = b.CheckIn.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOut.ToString("yyyy-MM-dd"),
                        TotalPrice = b.TotalPrice,
                        Currency = b.Currency,
                        HotelbedsReference = b.HotelbedsReference,
                        BookingStatus = b.BookingStatus.ToString(),
                        RoomCount = b.RoomCount,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                return new ServiceResponse<List<UserHotelBookingDto>>(userBookings, $"Found {userBookings.Count} bookings.");
            }
            catch (Exception ex)
            { return new ServiceResponse<List<UserHotelBookingDto>>($"Error retrieving bookings: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 6: GET BOOKING DETAIL (from Hotelbeds API, ownership check)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<BookingDetailResponseDto>> GetBookingDetailAsync(
            string reference, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return new ServiceResponse<BookingDetailResponseDto>("Booking reference is required.");
                if (string.IsNullOrWhiteSpace(userId))
                    return new ServiceResponse<BookingDetailResponseDto>("User must be authenticated.");

                // Rule 6: Direct projection on indexed HotelbedsReference column
                var dbBooking = await _bookingRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(b => b.HotelbedsReference == reference)
                    .Select(b => new { b.UserId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (dbBooking is null)
                    return new ServiceResponse<BookingDetailResponseDto>("Booking not found.");
                if (dbBooking.UserId != userId)
                    return new ServiceResponse<BookingDetailResponseDto>("You do not have permission to view this booking.");

                var httpResponse = await _httpClient.GetAsync($"bookings/{reference}", cancellationToken);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(httpResponse, cancellationToken);
                    return new ServiceResponse<BookingDetailResponseDto>($"Failed to retrieve booking: {err}");
                }

                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsBookingResponse>(JsonOptions, cancellationToken);
                if (apiResponse?.Booking is null)
                    return new ServiceResponse<BookingDetailResponseDto>("Booking details not available.");

                var b = apiResponse.Booking;
                var dto = new BookingDetailResponseDto
                {
                    Reference = b.Reference ?? string.Empty,
                    ClientReference = b.ClientReference ?? string.Empty,
                    Status = b.Status ?? string.Empty,
                    CreationDate = b.CreationDate ?? string.Empty,
                    HolderName = b.Holder is not null ? $"{b.Holder.Name} {b.Holder.Surname}".Trim() : string.Empty,
                    TotalNet = b.TotalNet ?? 0,
                    Currency = b.Currency ?? string.Empty,
                    CancellationReference = b.CancellationReference,
                    Hotel = b.Hotel is not null ? new BookingHotelDto
                    {
                        Code = b.Hotel.Code,
                        Name = b.Hotel.Name ?? string.Empty,
                        CheckIn = b.Hotel.CheckIn ?? string.Empty,
                        CheckOut = b.Hotel.CheckOut ?? string.Empty,
                        RoomCount = b.Hotel.Rooms?.Count ?? 0
                    } : null
                };

                return new ServiceResponse<BookingDetailResponseDto>(dto, "Booking details retrieved.");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<BookingDetailResponseDto>($"Network error: {ex.Message}"); }
            catch (Exception ex)
            { return new ServiceResponse<BookingDetailResponseDto>($"Unexpected error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT 7: CANCEL BOOKING (Hotelbeds API + DB update, ownership check)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<BookingCancellationResponseDto>> CancelBookingAsync(
            string reference, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return new ServiceResponse<BookingCancellationResponseDto>("Booking reference is required.");
                if (string.IsNullOrWhiteSpace(userId))
                    return new ServiceResponse<BookingCancellationResponseDto>("User must be authenticated.");

                // Rule 6: Direct projection on indexed HotelbedsReference column
                var dbBooking = await _bookingRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(b => b.HotelbedsReference == reference)
                    .Select(b => new { b.Id, b.UserId, b.BookingStatus })
                    .FirstOrDefaultAsync(cancellationToken);

                if (dbBooking is null)
                    return new ServiceResponse<BookingCancellationResponseDto>("Booking not found.");
                if (dbBooking.UserId != userId)
                    return new ServiceResponse<BookingCancellationResponseDto>("You do not have permission to cancel this booking.");
                if (dbBooking.BookingStatus == HotelBookingStatus.Refunded)
                    return new ServiceResponse<BookingCancellationResponseDto>("This booking has already been cancelled.");

                // Cancel via Hotelbeds API
                var httpResponse = await _httpClient.DeleteAsync($"bookings/{reference}", cancellationToken);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(httpResponse, cancellationToken);
                    return new ServiceResponse<BookingCancellationResponseDto>($"Cancellation failed: {err}");
                }

                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsBookingResponse>(JsonOptions, cancellationToken);

                // Rule 3: Set-based update directly on GetQueryable()
                await _bookingRepository.GetQueryable()
                    .Where(b => b.Id == dbBooking.Id && b.BookingStatus != HotelBookingStatus.Refunded)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BookingStatus, HotelBookingStatus.Refunded)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);

                var dto = new BookingCancellationResponseDto
                {
                    Reference = reference,
                    Status = apiResponse?.Booking?.Status ?? "CANCELLED",
                    CancellationReference = apiResponse?.Booking?.CancellationReference ?? string.Empty
                };

                return new ServiceResponse<BookingCancellationResponseDto>(dto, "Booking cancelled successfully.");
            }
            catch (HttpRequestException ex)
            { return new ServiceResponse<BookingCancellationResponseDto>($"Network error: {ex.Message}"); }
            catch (Exception ex)
            { return new ServiceResponse<BookingCancellationResponseDto>($"Unexpected error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE: Content API with caching
        // ═══════════════════════════════════════════════════════════════════

        private async Task<Dictionary<int, HotelbedsContentHotel>> GetHotelContentBatchAsync(
            List<int> hotelCodes, CancellationToken cancellationToken)
        {
            var result = new Dictionary<int, HotelbedsContentHotel>();
            var uncachedCodes = new List<int>();

            foreach (var code in hotelCodes)
            {
                var cacheKey = $"hotelbeds_content_{code}";
                if (_cache.TryGetValue<HotelbedsContentHotel>(cacheKey, out var cached) && cached is not null)
                    result[code] = cached;
                else
                    uncachedCodes.Add(code);
            }

            if (uncachedCodes.Count == 0) return result;

            try
            {
                var codes = string.Join(",", uncachedCodes);
                var contentUrl = $"{_settings.ContentApiBaseUrl}hotels?codes={codes}&fields=all&language=ENG&from=1&to={uncachedCodes.Count}";
                var httpResponse = await _httpClient.GetAsync(contentUrl, cancellationToken);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var contentResponse = await httpResponse.Content.ReadFromJsonAsync<HotelbedsContentResponse>(JsonOptions, cancellationToken);
                    if (contentResponse?.Hotels is not null)
                    {
                        foreach (var hotel in contentResponse.Hotels)
                        {
                            var cacheKey = $"hotelbeds_content_{hotel.Code}";
                            _cache.Set(cacheKey, hotel, ContentCacheDuration);
                            result[hotel.Code] = hotel;
                        }
                    }
                }
            }
            catch { /* Content API failure should not break search — images just won't appear */ }

            return result;
        }

        /// <summary>Maps Content API images to DTOs using 800px thumbnail URLs (for search cards).</summary>
        private static List<HotelImageDto> MapThumbnailImages(List<HotelbedsContentImage>? images)
        {
            if (images is null || images.Count == 0) return new();
            return images
                .OrderBy(i => i.VisualOrder).ThenBy(i => i.Order)
                .Select(i => new HotelImageDto
                {
                    Url = HotelbedsImageHelper.GetThumbnailUrl(i.Path),
                    Type = i.ImageTypeCode ?? string.Empty,
                    Order = i.VisualOrder
                }).ToList();
        }

        /// <summary>Maps Content API images to DTOs using max-resolution gallery URLs (for hotel detail pages).</summary>
        private static List<HotelImageDto> MapGalleryImages(List<HotelbedsContentImage>? images)
        {
            if (images is null || images.Count == 0) return new();
            return images
                .OrderBy(i => i.VisualOrder).ThenBy(i => i.Order)
                .Select(i => new HotelImageDto
                {
                    Url = HotelbedsImageHelper.GetGalleryImageUrl(i.Path),
                    Type = i.ImageTypeCode ?? string.Empty,
                    Order = i.VisualOrder
                }).ToList();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE: Request builders
        // ═══════════════════════════════════════════════════════════════════

        private static HotelbedsAvailabilityRequest BuildAvailabilityRequest(HotelAvailabilityRequestDto dto)
        {
            var req = new HotelbedsAvailabilityRequest
            {
                Stay = new HotelbedsStay { CheckIn = dto.CheckIn, CheckOut = dto.CheckOut },
                Occupancies = dto.Occupancies.Select(o =>
                {
                    var occ = new HotelbedsOccupancy { Rooms = 1, Adults = o.Adults, Children = o.Children };
                    if (o.Children > 0 && o.ChildrenAges is not null)
                    {
                        occ.Paxes = new List<HotelbedsPax>();
                        for (int i = 0; i < o.Adults; i++) occ.Paxes.Add(new HotelbedsPax { Type = "AD", Age = 30 });
                        foreach (var age in o.ChildrenAges) occ.Paxes.Add(new HotelbedsPax { Type = "CH", Age = age });
                    }
                    return occ;
                }).ToList()
            };
            if (!string.IsNullOrWhiteSpace(dto.DestinationCode))
                req.Destination = new HotelbedsDestination { Code = dto.DestinationCode };
            else if (dto.Latitude.HasValue && dto.Longitude.HasValue)
            {
                req.Geolocation = new HotelbedsGeolocation
                {
                    Latitude = dto.Latitude.Value,
                    Longitude = dto.Longitude.Value,
                    Radius = dto.RadiusInKm,
                    Unit = "km"
                };
            }
            else if (dto.HotelCodes is not null && dto.HotelCodes.Count > 0)
                req.Hotels = new HotelbedsHotelsFilter { Hotel = dto.HotelCodes };
            if (dto.MaxHotels > 0 || dto.MinCategory.HasValue || dto.MaxCategory.HasValue)
                req.Filter = new HotelbedsFilter { MaxHotels = dto.MaxHotels > 0 ? dto.MaxHotels : null, MinCategory = dto.MinCategory, MaxCategory = dto.MaxCategory };
            return req;
        }

        private static HotelbedsBookingRequest BuildBookingRequest(HotelBookingRequestDto dto, string clientRef)
        {
            return new HotelbedsBookingRequest
            {
                Holder = new HotelbedsHolder { Name = dto.HolderFirstName, Surname = dto.HolderLastName },
                ClientReference = clientRef,
                Remark = dto.Remark,
                Rooms = dto.Rooms.Select(r => new HotelbedsBookingRoom
                {
                    RateKey = r.RateKey,
                    Paxes = r.Paxes.Select(p => new HotelbedsBookingPax
                    { RoomId = p.RoomId, Type = p.Type, Name = p.Name, Surname = p.Surname, Age = p.Age }).ToList()
                }).ToList()
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE: Response mappers
        // ═══════════════════════════════════════════════════════════════════

        private static HotelAvailabilityResponseDto MapAvailabilityResponse(HotelbedsAvailabilityResponse api, decimal exchangeRate)
        {
            var hotels = api.Hotels?.Hotels ?? new();

            return new HotelAvailabilityResponseDto
            {
                TotalHotels = api.Hotels?.Total ?? 0,
                Hotels = hotels.Select(h => new HotelSearchResultDto
                {
                    Code = h.Code,
                    Name = h.Name ?? string.Empty,
                    CategoryCode = h.CategoryCode ?? string.Empty,
                    CategoryName = h.CategoryName ?? string.Empty,
                    DestinationCode = h.DestinationCode ?? string.Empty,
                    DestinationName = h.DestinationName ?? string.Empty,
                    Latitude = decimal.TryParse(h.Latitude, out var lat) ? lat : null,
                    Longitude = decimal.TryParse(h.Longitude, out var lng) ? lng : null,
                    MinRate = decimal.TryParse(h.MinRate, out var min) ? Math.Round(min * exchangeRate * CorporateMarkupMultiplier, 2) : 0,
                    MaxRate = decimal.TryParse(h.MaxRate, out var max) ? Math.Round(max * exchangeRate * CorporateMarkupMultiplier, 2) : 0,
                    Currency = DisplayCurrency
                }).ToList()
            };
        }

        private static List<AvailableRoomDto> MapRooms(
            List<HotelbedsRoom>? rooms,
            List<HotelbedsContentRoom>? contentRooms,
            List<HotelbedsContentImage>? contentImages,
            decimal exchangeRate,
            IReadOnlyDictionary<int, string>? facilityLookup = null)
        {
            if (rooms is null) return new();
            return rooms.Select(r =>
            {
                var roomName = r.Name ?? string.Empty;
                var contentRoom = contentRooms?.FirstOrDefault(cr => cr.RoomCode == r.Code);
                if (contentRoom != null && !string.IsNullOrWhiteSpace(contentRoom.Description))
                {
                    roomName = contentRoom.Description;
                }

                var habImages = contentImages?
                    .Where(i => i.ImageTypeCode == "HAB" && i.RoomCode == r.Code)
                    .Select(i => new HotelImageDto
                    {
                        Url = HotelbedsImageHelper.GetGalleryImageUrl(i.Path),
                        Type = "HAB",
                        Order = i.VisualOrder
                    }).ToList() ?? new List<HotelImageDto>();

                var roomFacilities = new List<string>();
                if (contentRoom?.RoomFacilities is not null && facilityLookup is not null)
                {
                    roomFacilities = contentRoom.RoomFacilities
                        .Where(rf => rf.IndYesOrNo != false)
                        .Select(rf => facilityLookup.TryGetValue(rf.FacilityCode, out var desc) ? desc : null)
                        .Where(desc => !string.IsNullOrWhiteSpace(desc))
                        .Distinct()
                        .OrderBy(desc => desc)
                        .ToList()!;
                }

                return new AvailableRoomDto
                {
                    Code = r.Code ?? string.Empty,
                    Name = roomName,
                    Images = habImages,
                    RoomFacilities = roomFacilities,
                    Rates = r.Rates?
                        .GroupBy(rate => new
                        {
                            Board = rate.BoardCode ?? string.Empty,
                            Class = rate.RateClass ?? string.Empty
                        })
                        .Select(group => group
                            .OrderBy(rate => decimal.TryParse(rate.Net, out var net) ? net : decimal.MaxValue)
                            .First()
                        )
                        .Select(rate => new RateDto
                        {
                            RateKey = rate.RateKey ?? string.Empty,
                            RateClass = rate.RateClass ?? string.Empty,
                            Price = decimal.TryParse(rate.Net, out var net) ? Math.Round(net * CorporateMarkupMultiplier * exchangeRate, 2) : 0m,
                            BoardCode = rate.BoardCode ?? string.Empty,
                            BoardName = rate.BoardName ?? string.Empty,
                            Allotment = rate.Allotment,
                            CancellationPolicies = rate.CancellationPolicies?.Select(cp => new CancellationPolicyDto
                            {
                                Amount = decimal.TryParse(cp.Amount, out var amt) ? Math.Round(amt * exchangeRate, 2) : 0m,
                                From = cp.From ?? string.Empty
                            }).ToList() ?? new()
                        }).ToList() ?? new()
                };
            }).ToList();
        }

        private static HotelCheckRateResponseDto MapCheckRateResponse(HotelbedsCheckRateResponse api, decimal exchangeRate)
        {
            var h = api.Hotel;
            if (h is null) return new();
            return new HotelCheckRateResponseDto
            {
                Hotel = new CheckRateHotelDto
                {
                    Code = h.Code,
                    Name = h.Name ?? string.Empty,
                    CategoryCode = h.CategoryCode ?? string.Empty,
                    DestinationCode = h.DestinationCode ?? string.Empty,
                    TotalPrice = decimal.TryParse(h.TotalNet, out var t) ? Math.Round(t * CorporateMarkupMultiplier * exchangeRate, 2) : 0,
                    Currency = DisplayCurrency,
                    CheckIn = h.CheckIn ?? string.Empty,
                    CheckOut = h.CheckOut ?? string.Empty,
                    Rooms = MapRooms(h.Rooms, null, null, exchangeRate)
                }
            };
        }

        private static HotelBookingResponseDto MapBookingResponse(HotelbedsBookingResponse api, decimal exchangeRate)
        {
            var b = api.Booking;
            if (b is null) return new();
            return new HotelBookingResponseDto
            {
                BookingReference = b.Reference ?? string.Empty,
                ClientReference = b.ClientReference ?? string.Empty,
                Status = b.Status ?? string.Empty,
                TotalPrice = b.TotalNet.HasValue ? Math.Round(b.TotalNet.Value * CorporateMarkupMultiplier * exchangeRate, 2) : 0,
                Currency = DisplayCurrency,
                CreationDate = b.CreationDate ?? string.Empty,
                Hotel = b.Hotel is not null ? new BookingHotelDto
                {
                    Code = b.Hotel.Code,
                    Name = b.Hotel.Name ?? string.Empty,
                    CheckIn = b.Hotel.CheckIn ?? string.Empty,
                    CheckOut = b.Hotel.CheckOut ?? string.Empty,
                    RoomCount = b.Hotel.Rooms?.Count ?? 0
                } : null
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE: Error extraction
        // ═══════════════════════════════════════════════════════════════════

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            try
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var err = JsonSerializer.Deserialize<HotelbedsErrorResponse>(body, JsonOptions);
                    if (err?.Error is not null) return $"[{err.Error.Code}] {err.Error.Message}";
                }
            }
            catch { }
            return $"HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase})";
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENDPOINT: INIT CHECKOUT (Stripe Payment Intent with Idempotency & UoW)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<CheckoutResponseDto>> InitCheckoutAsync(
            HotelBookingRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // ── Validation ────────────────────────────────────────────────
                if (request is null)
                    return new ServiceResponse<CheckoutResponseDto>("Booking request cannot be null.");
                if (string.IsNullOrWhiteSpace(request.HolderFirstName) || string.IsNullOrWhiteSpace(request.HolderLastName))
                    return new ServiceResponse<CheckoutResponseDto>("Holder's first and last name are required.");
                if (request.Rooms is null || request.Rooms.Count == 0)
                    return new ServiceResponse<CheckoutResponseDto>("At least one room with guest details is required.");
                if (string.IsNullOrWhiteSpace(userId))
                    return new ServiceResponse<CheckoutResponseDto>("User must be authenticated.");

                for (int i = 0; i < request.Rooms.Count; i++)
                {
                    var room = request.Rooms[i];
                    if (string.IsNullOrWhiteSpace(room.RateKey))
                        return new ServiceResponse<CheckoutResponseDto>($"Room {i + 1} is missing a rate key.");
                    if (room.Paxes is null || room.Paxes.Count == 0)
                        return new ServiceResponse<CheckoutResponseDto>($"Room {i + 1} must have at least one guest.");
                    if (!room.Paxes.Any(p => p.Type == "AD"))
                        return new ServiceResponse<CheckoutResponseDto>($"Room {i + 1} must have at least one adult (Type = \"AD\").");
                }

                // ── Step 1: Validate ALL rates via Hotelbeds CheckRate ─────────
                var checkRateApiRequest = new HotelbedsCheckRateRequest
                {
                    Rooms = request.Rooms
                        .Select(r => new HotelbedsCheckRateRoom { RateKey = r.RateKey })
                        .ToList()
                };
                var checkRateResponse = await _httpClient.PostAsJsonAsync("checkrates", checkRateApiRequest, JsonOptions, cancellationToken);

                if (!checkRateResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(checkRateResponse, cancellationToken);
                    return new ServiceResponse<CheckoutResponseDto>($"Rate validation failed: {err}");
                }

                var checkRateResult = await checkRateResponse.Content.ReadFromJsonAsync<HotelbedsCheckRateResponse>(JsonOptions, cancellationToken);
                if (checkRateResult?.Hotel is null)
                    return new ServiceResponse<CheckoutResponseDto>("The selected rate(s) are no longer available. Please search again.");

                // ── Step 2: Calculate final price (freeze exchange rate + markup) ──
                var wholesaleNet = decimal.TryParse(checkRateResult.Hotel.TotalNet, out var rawNet) ? rawNet : 0m;
                if (wholesaleNet <= 0)
                    return new ServiceResponse<CheckoutResponseDto>("Invalid price returned from rate validation.");

                var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync(
                    WholesaleCurrency, DisplayCurrency, cancellationToken);
                var finalPrice = Math.Round(wholesaleNet * CorporateMarkupMultiplier * exchangeRate, 2);

                var h = checkRateResult.Hotel;
                var checkInDate = DateOnly.TryParse(h.CheckIn, out var ci) ? ci : DateOnly.FromDateTime(DateTime.UtcNow);
                var checkOutDate = DateOnly.TryParse(h.CheckOut, out var co) ? co : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
                var combinedRateKey = string.Join(",", request.Rooms.Select(r => r.RateKey));
                var guestDataJson = JsonSerializer.Serialize(request, JsonOptions);
                var now = DateTime.UtcNow;
                var staleCutoff = now.AddMinutes(-15);

                // Rule 3: Clean up any expired PendingPayment checkouts (> 15 mins old) for this user + hotel + stay
                await _bookingRepository.GetQueryable()
                    .Where(b => b.UserId == userId &&
                                b.HotelId == h.Code &&
                                b.CheckIn == checkInDate &&
                                b.CheckOut == checkOutDate &&
                                b.BookingStatus == HotelBookingStatus.PendingPayment &&
                                b.CreatedAt < staleCutoff)
                    .ExecuteDeleteAsync(cancellationToken);

                // Rule 6: Check if an active PendingPayment booking already exists for this user + hotel + stay
                var existingPending = await _bookingRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(b => b.UserId == userId &&
                                b.HotelId == h.Code &&
                                b.CheckIn == checkInDate &&
                                b.CheckOut == checkOutDate &&
                                b.BookingStatus == HotelBookingStatus.PendingPayment)
                    .Select(b => new { b.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                Guid bookingId;
                if (existingPending != null)
                {
                    bookingId = existingPending.Id;

                    // Rule 3: Update rate/guest details if the user modified rooms within the same checkout window
                    await _bookingRepository.GetQueryable()
                        .Where(b => b.Id == bookingId && b.BookingStatus == HotelBookingStatus.PendingPayment)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(b => b.TotalPrice, finalPrice)
                            .SetProperty(b => b.RateKey, combinedRateKey)
                            .SetProperty(b => b.RoomCount, request.Rooms.Count)
                            .SetProperty(b => b.GuestDataJson, guestDataJson)
                            .SetProperty(b => b.WholesaleNetEur, wholesaleNet)
                            .SetProperty(b => b.ExchangeRateAtCheckout, exchangeRate)
                            .SetProperty(b => b.UpdatedAt, now), cancellationToken);
                }
                else
                {
                    // ── Step 3: Persist PendingPayment record inside UoW ───────────
                    var booking = new HotelBooking
                    {
                        UserId = userId,
                        HotelId = h.Code,
                        HotelName = h.Name ?? string.Empty,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        TotalPrice = finalPrice,
                        Currency = DisplayCurrency,
                        RateKey = combinedRateKey,
                        BookingStatus = HotelBookingStatus.PendingPayment,
                        RoomCount = request.Rooms.Count,
                        GuestDataJson = guestDataJson,
                        WholesaleNetEur = wholesaleNet,
                        ExchangeRateAtCheckout = exchangeRate,
                        CreatedAt = now
                    };

                    await _unitOfWork.ExecuteAsync(async () =>
                    {
                        await _bookingRepository.AddAsync(booking, cancellationToken);
                    }, cancellationToken);

                    bookingId = booking.Id;
                }

                // ── Step 4: Create Stripe PaymentIntent (with Deterministic IdempotencyKey) ──
                var amountInCents = (long)Math.Round(finalPrice * 100m, MidpointRounding.AwayFromZero);

                try
                {
                    var piResult = await _paymentGateway.CreatePaymentIntentAsync(
                        amountInCents, DisplayCurrency.ToLowerInvariant(), bookingId, "Hotel", cancellationToken);

                    // ── Step 5: Link PaymentIntent to our booking via Rule 3 ExecuteUpdateAsync ─
                    await _bookingRepository.GetQueryable()
                        .Where(b => b.Id == bookingId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(b => b.StripePaymentIntentId, piResult.Id)
                            .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);

                    var responseDto = new CheckoutResponseDto
                    {
                        ClientSecret = piResult.ClientSecret,
                        BookingId = bookingId,
                        TotalPrice = finalPrice,
                        Currency = DisplayCurrency
                    };

                    return new ServiceResponse<CheckoutResponseDto>(responseDto, "Checkout initialized. Proceed to payment.");
                }
                catch (Exception stripeEx)
                {
                    _logger.LogError(stripeEx,
                        "Stripe PaymentIntent creation failed for BookingId {BookingId}. Rolling back DB record.",
                        bookingId);
                    try
                    {
                        await _bookingRepository.GetQueryable()
                            .Where(b => b.Id == bookingId && b.BookingStatus == HotelBookingStatus.PendingPayment)
                            .ExecuteDeleteAsync(cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx,
                            "Failed to rollback orphaned PendingPayment record for BookingId {BookingId}.",
                            bookingId);
                    }
                    return new ServiceResponse<CheckoutResponseDto>($"Payment initialization failed: {stripeEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout initialization.");
                return new ServiceResponse<CheckoutResponseDto>($"Error during checkout initialization: {ex.Message}");
            }
        }
    }
}
