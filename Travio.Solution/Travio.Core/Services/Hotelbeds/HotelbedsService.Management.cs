using System.Net.Http.Json;
using System.Text.Json;
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

                var bookings = await _bookingRepository.ListAsync(cancellationToken);
                var userBookings = bookings
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
                    }).ToList();

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

                // Ownership check from DB
                var allBookings = await _bookingRepository.ListAsync(cancellationToken);
                var dbBooking = allBookings.FirstOrDefault(b => b.HotelbedsReference == reference);
                if (dbBooking is null)
                    return new ServiceResponse<BookingDetailResponseDto>("Booking not found.");
                if (dbBooking.UserId != userId)
                    return new ServiceResponse<BookingDetailResponseDto>("You do not have permission to view this booking.");

                // Get live details from Hotelbeds
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
                    TotalNet = decimal.TryParse(b.TotalNet, out var net) ? net : 0,
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

                // Ownership check
                var allBookings = await _bookingRepository.ListAsync(cancellationToken);
                var dbBooking = allBookings.FirstOrDefault(b => b.HotelbedsReference == reference);
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

                // Update DB status
                dbBooking.BookingStatus = HotelBookingStatus.Refunded;
                dbBooking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(dbBooking, cancellationToken);
                await _bookingRepository.SaveChangesAsync(cancellationToken);

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
                // THE FALLBACK: Use Geolocation instead of a Destination Code
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
                    MinRate = decimal.TryParse(h.MinRate, out var min) ? Math.Round(min * exchangeRate * 1.15m, 2) : 0,
                    MaxRate = decimal.TryParse(h.MaxRate, out var max) ? Math.Round(max * exchangeRate * 1.15m, 2) : 0,
                    Currency = "USD"
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
                // Map human-readable room name from Content API wildcards
                var roomName = r.Name ?? string.Empty;
                var contentRoom = contentRooms?.FirstOrDefault(cr => cr.RoomCode == r.Code);
                if (contentRoom != null && !string.IsNullOrWhiteSpace(contentRoom.Description))
                {
                    roomName = contentRoom.Description;
                }

                // Map HAB photos
                var habImages = contentImages?
                    .Where(i => i.ImageTypeCode == "HAB" && i.RoomCode == r.Code)
                    .Select(i => new HotelImageDto
                    {
                        Url = HotelbedsImageHelper.GetGalleryImageUrl(i.Path),
                        Type = "HAB",
                        Order = i.VisualOrder
                    }).ToList() ?? new List<HotelImageDto>();

                // Map room facilities — cross-reference Content API facilityCode against our local DB
                var roomFacilities = new List<string>();
                if (contentRoom?.RoomFacilities is not null && facilityLookup is not null)
                {
                    roomFacilities = contentRoom.RoomFacilities
                        .Where(rf => rf.IndYesOrNo != false) // Exclude explicitly disabled facilities
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
                    Rates = r.Rates?.Select(rate => new RateDto
                    {
                        RateKey = rate.RateKey ?? string.Empty,
                        RateClass = rate.RateClass ?? string.Empty,
                        Price = decimal.TryParse(rate.Net, out var net) ? Math.Round(net * 1.15m * exchangeRate, 2) : 0,
                        BoardCode = rate.BoardCode ?? string.Empty,
                        BoardName = rate.BoardName ?? string.Empty,
                        Allotment = rate.Allotment,
                        CancellationPolicies = rate.CancellationPolicies?.Select(cp => new CancellationPolicyDto
                        { Amount = decimal.TryParse(cp.Amount, out var amt) ? Math.Round(amt * exchangeRate, 2) : 0, From = cp.From ?? string.Empty }).ToList() ?? new()
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
                    TotalPrice = decimal.TryParse(h.TotalNet, out var t) ? Math.Round(t * 1.15m * exchangeRate, 2) : 0,
                    Currency = "USD",
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
                TotalPrice = decimal.TryParse(b.TotalNet, out var t) ? Math.Round(t * 1.15m * exchangeRate, 2) : 0,
                Currency = "USD",
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
        // PRIVATE: DB persistence & error extraction
        // ═══════════════════════════════════════════════════════════════════

        private async Task PersistBookingRecordAsync(string userId, HotelBookingRequestDto request,
            HotelbedsBookingDetail? detail, HotelBookingStatus status, decimal exchangeRate, CancellationToken ct)
        {
            try
            {
                var booking = new HotelBooking
                {
                    UserId = userId,
                    HotelId = detail?.Hotel?.Code ?? 0,
                    HotelName = detail?.Hotel?.Name ?? string.Empty,
                    CheckIn = DateOnly.TryParse(detail?.Hotel?.CheckIn, out var ci) ? ci : DateOnly.FromDateTime(DateTime.UtcNow),
                    CheckOut = DateOnly.TryParse(detail?.Hotel?.CheckOut, out var co) ? co : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    TotalPrice = decimal.TryParse(detail?.TotalNet, out var p) ? Math.Round(p * exchangeRate * 1.15m, 2) : 0,
                    Currency = "USD",
                    HotelbedsReference = detail?.Reference,
                    RateKey = request.RateKey,
                    BookingStatus = status,
                    RoomCount = request.Rooms?.Count ?? 1,
                    CreatedAt = DateTime.UtcNow
                };
                await _bookingRepository.AddAsync(booking, ct);
                await _bookingRepository.SaveChangesAsync(ct);
            }
            catch { /* Log in production — don't fail the booking response */ }
        }

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
        // ENDPOINT: INIT CHECKOUT (Stripe Payment Intent)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<ServiceResponse<CheckoutResponseDto>> InitCheckoutAsync(
            HotelBookingRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // ── Validation ────────────────────────────────────────────────
                if (request is null)
                    return new ServiceResponse<CheckoutResponseDto>("Booking request cannot be null.");
                if (string.IsNullOrWhiteSpace(request.RateKey))
                    return new ServiceResponse<CheckoutResponseDto>("Rate key is required.");
                if (string.IsNullOrWhiteSpace(request.HolderFirstName) || string.IsNullOrWhiteSpace(request.HolderLastName))
                    return new ServiceResponse<CheckoutResponseDto>("Holder's first and last name are required.");
                if (request.Rooms is null || request.Rooms.Count == 0)
                    return new ServiceResponse<CheckoutResponseDto>("At least one room with guest details is required.");
                if (string.IsNullOrWhiteSpace(userId))
                    return new ServiceResponse<CheckoutResponseDto>("User must be authenticated.");

                // ── Step 1: Validate the rate via Hotelbeds CheckRate ─────────
                var checkRateApiRequest = new HotelbedsCheckRateRequest
                {
                    Rooms = new List<HotelbedsCheckRateRoom> { new() { RateKey = request.RateKey } }
                };
                var checkRateResponse = await _httpClient.PostAsJsonAsync("checkrates", checkRateApiRequest, JsonOptions, cancellationToken);

                if (!checkRateResponse.IsSuccessStatusCode)
                {
                    var err = await ExtractErrorMessageAsync(checkRateResponse, cancellationToken);
                    return new ServiceResponse<CheckoutResponseDto>($"Rate validation failed: {err}");
                }

                var checkRateResult = await checkRateResponse.Content.ReadFromJsonAsync<HotelbedsCheckRateResponse>(JsonOptions, cancellationToken);
                if (checkRateResult?.Hotel is null)
                    return new ServiceResponse<CheckoutResponseDto>("The selected rate is no longer available. Please search again.");

                // ── Step 2: Calculate final price (15% markup + currency conversion) ──
                var wholesaleNet = decimal.TryParse(checkRateResult.Hotel.TotalNet, out var rawNet) ? rawNet : 0m;
                if (wholesaleNet <= 0)
                    return new ServiceResponse<CheckoutResponseDto>("Invalid price returned from rate validation.");

                var exchangeRate = await _currencyExchangeService.GetExchangeRateAsync("EUR", "USD", cancellationToken);
                var finalPrice = Math.Round(wholesaleNet * 1.15m * exchangeRate, 2); // Markup + conversion

                // ── Step 3: Persist PendingPayment record ─────────────────────
                var h = checkRateResult.Hotel;
                var booking = new HotelBooking
                {
                    UserId = userId,
                    HotelId = h.Code,
                    HotelName = h.Name ?? string.Empty,
                    CheckIn = DateOnly.TryParse(h.CheckIn, out var ci) ? ci : DateOnly.FromDateTime(DateTime.UtcNow),
                    CheckOut = DateOnly.TryParse(h.CheckOut, out var co) ? co : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    TotalPrice = finalPrice,
                    Currency = "USD",
                    RateKey = request.RateKey,
                    BookingStatus = HotelBookingStatus.PendingPayment,
                    RoomCount = request.Rooms.Count,
                    GuestDataJson = JsonSerializer.Serialize(request, JsonOptions),
                    CreatedAt = DateTime.UtcNow
                };

                await _bookingRepository.AddAsync(booking, cancellationToken);
                await _bookingRepository.SaveChangesAsync(cancellationToken);

                // ── Step 4: Create Stripe PaymentIntent ───────────────────────
                // CRITICAL: Stripe expects amounts in the smallest currency unit (cents for USD).
                var amountInCents = (long)(finalPrice * 100);

                var stripeService = new Stripe.PaymentIntentService();
                var stripeOptions = new Stripe.PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "usd",
                    Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", booking.Id.ToString() },
                        { "BookingType", "Hotel" }
                    }
                };

                var paymentIntent = await stripeService.CreateAsync(stripeOptions, cancellationToken: cancellationToken);

                // ── Step 5: Link PaymentIntent to our booking ─────────────────
                booking.StripePaymentIntentId = paymentIntent.Id;
                await _bookingRepository.UpdateAsync(booking, cancellationToken);
                await _bookingRepository.SaveChangesAsync(cancellationToken);

                var responseDto = new CheckoutResponseDto
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    BookingId = booking.Id,
                    TotalPrice = finalPrice,
                    Currency = "USD"
                };

                return new ServiceResponse<CheckoutResponseDto>(responseDto, "Checkout initialized. Proceed to payment.");
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Stripe error during checkout initialization.");
                return new ServiceResponse<CheckoutResponseDto>($"Payment initialization failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout initialization.");
                return new ServiceResponse<CheckoutResponseDto>($"Error during checkout initialization: {ex.Message}");
            }
        }
    }
}
