using Duffel.ApiClient;
using Duffel.ApiClient.Models;
using Duffel.ApiClient.Models.Requests;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Services.DuffelFlights
{
    public class DuffelFlightBookingService : IDuffelFlightBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly IGenericRepository<FlightBooking> _bookingRepo;
        private readonly IDuffelFlightBookingService _flightService;

        // Inject the standard HttpClient we just configured in Program.cs
        public DuffelFlightBookingService(HttpClient httpClient, IGenericRepository<FlightBooking> bookingRepo)
        {
            _httpClient = httpClient;
            _bookingRepo = bookingRepo;
          
        }

        public async Task<ServiceResponse<List<FlightSearchResponseDto>>> SearchFlightsAsync(FlightSearchRequestDto request)
        {
            try
            {
                var slice = new
                {
                    origin = request.Origin,
                    destination = request.Destination,
                    departure_date = request.DepartureDate,
                    max_connections = request.MaxStops
                };

                var payload = new
                {
                    data = new
                    {
                        cabin_class = string.IsNullOrWhiteSpace(request.CabinClass) ? "economy" : request.CabinClass.ToLower(),
                        passengers = Enumerable.Repeat(new { type = "adult" }, request.Adults).ToList(),
                        slices = new[] { slice }
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("air/offer_requests?return_offers=true", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ServiceResponse<List<FlightSearchResponseDto>>
                    {
                        Success = false,
                        Message = $"Duffel API Error: {responseString}"
                    };
                }
                using var jsonDoc = JsonDocument.Parse(responseString);
                var offers = jsonDoc.RootElement.GetProperty("data").GetProperty("offers").EnumerateArray();

                var flights = new List<FlightSearchResponseDto>();

                foreach (var offer in offers)
                {
                    var slices = offer.GetProperty("slices").EnumerateArray();
                    var firstSlice = slices.FirstOrDefault();

                    if (firstSlice.ValueKind == JsonValueKind.Undefined) continue;

                    var duffelSegments = firstSlice.GetProperty("segments").EnumerateArray().ToList();
                    var mappedSegments = new List<FlightSegmentDto>();

                    foreach (var seg in duffelSegments)
                    {
                        var carrier = seg.GetProperty("operating_carrier");

                        // 1. Safely extract the logo, with a clean fallback placeholder
                        string logoUrl = carrier.TryGetProperty("logo_symbol_url", out var logo) && logo.ValueKind != JsonValueKind.Null
                            ? logo.GetString()
                            : "https://placehold.co/400x400/000000/ffffff?text=" + carrier.GetProperty("name").GetString().Replace(" ", "+");

                        mappedSegments.Add(new FlightSegmentDto
                        {
                            Origin = seg.GetProperty("origin").GetProperty("iata_code").GetString(),
                            OriginCityName = seg.GetProperty("origin").GetProperty("city_name").GetString(),

                            Destination = seg.GetProperty("destination").GetProperty("iata_code").GetString(),
                            DestinationCityName = seg.GetProperty("destination").GetProperty("city_name").GetString(),

                            DepartureTime = seg.GetProperty("departing_at").GetDateTime(),
                            ArrivalTime = seg.GetProperty("arriving_at").GetDateTime(),

                            AirlineName = carrier.GetProperty("name").GetString(),

                            // --- ADD THE LOGO TO THE SEGMENT ---
                            AirlineLogoUrl = logoUrl,

                            FlightNumber = carrier.GetProperty("iata_code").GetString() + " " +
                                           seg.GetProperty("operating_carrier_flight_number").GetString(),

                            SegmentDuration = seg.GetProperty("duration").GetString()
                        });
                    }

                    // ... further down, when you assemble the main FlightSearchResponseDto ...

                    int calculatedStops = mappedSegments.Count - 1;

                    flights.Add(new FlightSearchResponseDto
                    {
                        OfferId = offer.GetProperty("id").GetString(),

                        TotalOrigin = firstSlice.GetProperty("origin").GetProperty("iata_code").GetString(),
                        OriginCityName = firstSlice.GetProperty("origin").GetProperty("city_name").GetString(),

                        TotalDestination = firstSlice.GetProperty("destination").GetProperty("iata_code").GetString(),
                        DestinationCityName = firstSlice.GetProperty("destination").GetProperty("city_name").GetString(),

                        TotalPrice = decimal.Parse(offer.GetProperty("total_amount").GetString()),
                        Currency = offer.GetProperty("total_currency").GetString(),
                        TotalDuration = firstSlice.GetProperty("duration").GetString(),
                        Stops = calculatedStops < 0 ? 0 : calculatedStops,

                        AirlineLogoUrl = mappedSegments.FirstOrDefault()?.AirlineLogoUrl,

                        Segments = mappedSegments
                    });
                }

                return new ServiceResponse<List<FlightSearchResponseDto>>
                {
                    Success = true,
                    Message = "Flights retrieved successfully.",
                    Data = flights.OrderBy(f => f.TotalPrice).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<FlightSearchResponseDto>>
                {
                    Success = false,
                    Message = $"An error occurred while searching for flights: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResponse<List<TopFlightOfferDto>>> GetTopOffersAsync()
        {
            try
            {
                // 1. Define the dynamic routes for the UI Carousel
                var popularRoutes = new List<(string Name, string Dest, string ImageUrl)>
        {
            ("Paris", "CDG", "https://plus.unsplash.com/premium_photo-1719581957038-0121108b9455?q=80&w=687&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"),
            ("Dubai", "DXB", "https://images.unsplash.com/photo-1700397801373-3c13a56d6cc3?q=80&w=735&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"),
            ("London", "LHR", "https://images.unsplash.com/photo-1569865867048-34cfce8d58fe?q=80&w=678&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D")
        };

                var futureDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

                // 2. Fan-out: Request all destinations from Duffel in parallel
                var tasks = popularRoutes.Select(async route =>
                {
                    var request = new FlightSearchRequestDto
                    {
                        Origin = "CAI",
                        Destination = route.Dest,
                        DepartureDate = futureDate,
                        Adults = 1,
                        MaxStops = 1
                    };

                    // Call your existing search method
                    var response = await SearchFlightsAsync(request);

                    if (response.Success && response.Data != null && response.Data.Any())
                    {
                        // Grab the absolute cheapest flight for this specific destination
                        var cheapestFlight = response.Data.First();
                        var firstSegment = cheapestFlight.Segments?.FirstOrDefault();

                        // 3. Map to the highly-detailed DTO
                        return new TopFlightOfferDto
                        {
                            OfferId = cheapestFlight.OfferId,
                            AirlineName = firstSegment?.AirlineName ?? "Multiple Airlines",
                            ImageUrl = route.ImageUrl,
                            AirlineLogoUrl = cheapestFlight.AirlineLogoUrl,


                            Origin = "CAI",
                            OriginCityName = "Cairo",

                            Destination = route.Dest,
                            DestinationCityName = route.Name,



                            Duration = cheapestFlight.TotalDuration ?? "N/A",
                            DepartureTime = firstSegment.DepartureTime,
                            ArrivalTime = firstSegment.ArrivalTime,

                            FlightNumber = firstSegment?.FlightNumber ?? "Unknown",


                            Stops = cheapestFlight.Segments != null ? Math.Max(0, cheapestFlight.Segments.Count - 1) : 0,


                            CheapestPrice = cheapestFlight.TotalPrice,
                            Currency = cheapestFlight.Currency
                        };
                    }

                    return null;
                });


                var results = await Task.WhenAll(tasks);


                var topOffers = results.Where(r => r != null).ToList();

                return new ServiceResponse<List<TopFlightOfferDto>>
                {
                    Success = true,
                    Message = "Top offers retrieved successfully.",
                    Data = topOffers
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<TopFlightOfferDto>>
                {
                    Success = false,
                    Message = $"An error occurred while fetching top offers: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResponse<FlightDetailsDto>> GetFlightDetailsAsync(string offerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"air/offers/{offerId}");
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ServiceResponse<FlightDetailsDto>
                    {
                        Success = false,
                        Message = "Could not retrieve flight details. The offer may have expired."
                    };
                }

                using var jsonDoc = JsonDocument.Parse(jsonString);
                var data = jsonDoc.RootElement.GetProperty("data");
                var firstSlice = data.GetProperty("slices").EnumerateArray().FirstOrDefault();

                // 1. Parse Baggage Rules
                int checkedBags = 0;
                var firstPassenger = data.GetProperty("passengers").EnumerateArray().FirstOrDefault();
                // Duffel usually puts baggage info inside the passenger or segment object depending on the airline
                // For safety, we check the first segment's passenger array if available
                var passengerSegment = firstSlice.GetProperty("segments").EnumerateArray().First()
                                        .GetProperty("passengers").EnumerateArray().FirstOrDefault();

                if (passengerSegment.ValueKind != JsonValueKind.Undefined && passengerSegment.TryGetProperty("baggages", out var baggages))
                {
                    foreach (var bag in baggages.EnumerateArray())
                    {
                        if (bag.GetProperty("type").GetString() == "checked")
                        {
                            checkedBags += bag.GetProperty("quantity").GetInt32();
                        }
                    }
                }

                // 2. Parse Refund Rules Safely
                bool isRefundable = false;
                decimal? refundPenalty = null;

                if (data.TryGetProperty("conditions", out var conditions) && conditions.ValueKind != JsonValueKind.Null)
                {
                    if (conditions.TryGetProperty("refund_before_departure", out var refundCondition) && refundCondition.ValueKind != JsonValueKind.Null)
                    {
                        isRefundable = refundCondition.TryGetProperty("allowed", out var allowed) && allowed.GetBoolean();

                        if (isRefundable && refundCondition.TryGetProperty("penalty_amount", out var penalty) && penalty.ValueKind != JsonValueKind.Null)
                        {
                            refundPenalty = decimal.Parse(penalty.GetString());
                        }
                    }
                }

                // 3. Parse Segments and Aircraft details
                var flightSegments = new List<FlightSegmentDetailsDto>();
                foreach (var segment in firstSlice.GetProperty("segments").EnumerateArray())
                {
                    var carrier = segment.GetProperty("operating_carrier");

                    string logoUrl = carrier.TryGetProperty("logo_symbol_url", out var logo) && logo.ValueKind != JsonValueKind.Null
                        ? logo.GetString()
                        : "https://placehold.co/400x400/000000/ffffff?text=Airline";

                    string aircraftName = "Aircraft Info Unavailable";
                    if (segment.TryGetProperty("aircraft", out var aircraft) && aircraft.ValueKind != JsonValueKind.Null)
                    {
                        aircraftName = aircraft.GetProperty("name").GetString();
                    }

                    flightSegments.Add(new FlightSegmentDetailsDto
                    {
                        AirlineName = carrier.GetProperty("name").GetString(),
                        AirlineLogoUrl = logoUrl,
                        FlightNumber = carrier.GetProperty("iata_code").GetString() + " " + segment.GetProperty("operating_carrier_flight_number").GetString(),
                        AircraftName = aircraftName,

                        OriginAirport = segment.GetProperty("origin").GetProperty("iata_code").GetString(),
                        OriginCityName = segment.GetProperty("origin").GetProperty("city_name").GetString(),
                        DepartureTime = segment.GetProperty("departing_at").GetString(),

                        DestinationAirport = segment.GetProperty("destination").GetProperty("iata_code").GetString(),
                        DestinationCityName = segment.GetProperty("destination").GetProperty("city_name").GetString(),
                        ArrivalTime = segment.GetProperty("arriving_at").GetString(),

                        SegmentDuration = segment.GetProperty("duration").GetString()
                    });
                }

                // 4. Assemble the Final DTO
                var details = new FlightDetailsDto
                {
                    OfferId = data.GetProperty("id").GetString(),
                    TotalPrice = decimal.Parse(data.GetProperty("total_amount").GetString()),
                    TaxAmount = decimal.Parse(data.GetProperty("tax_amount").GetString()),
                    Currency = data.GetProperty("total_currency").GetString(),
                    TotalDuration = firstSlice.GetProperty("duration").GetString(),
                    OriginCityName = firstSlice.GetProperty("origin").GetProperty("city_name").GetString(),

                    DestinationCityName = firstSlice.GetProperty("destination").GetProperty("city_name").GetString(),
                    CheckedBags = checkedBags,
                    IsRefundable = isRefundable,
                    RefundPenaltyAmount = refundPenalty,
                    Segments = flightSegments
                };

                return new ServiceResponse<FlightDetailsDto>
                {
                    Success = true,
                    Message = "Flight details retrieved successfully.",
                    Data = details
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<FlightDetailsDto>
                {
                    Success = false,
                    Message = $"Error parsing flight details: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<FlightOrderResponseDto>> CreateOrderAsync(FlightOrderRequestDto request)
        {
            try
            {

                var duffelPassengers = request.Passengers.Select(p => new
                {
                    type = "adult",
                    title = p.Title.ToLower(),
                    given_name = p.GivenName,
                    family_name = p.FamilyName,
                    born_on = p.BornOn,
                    email = p.Email,
                    phone_number = p.PhoneNumber,
                    gender = p.Gender.ToLower()
                }).ToList();

                var payload = new
                {
                    data = new
                    {
                        type = "instant",
                        selected_offers = new[] { request.OfferId },
                        passengers = duffelPassengers
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");


                var response = await _httpClient.PostAsync("air/orders", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {

                    return new ServiceResponse<FlightOrderResponseDto>
                    {
                        Success = false,
                        Message = $"Failed to create order: {responseString}"
                    };
                }


                using var jsonDoc = JsonDocument.Parse(responseString);
                var data = jsonDoc.RootElement.GetProperty("data");


                string pnr = data.TryGetProperty("booking_reference", out var bookingRef) && bookingRef.ValueKind != JsonValueKind.Null
                    ? bookingRef.GetString()
                    : "PENDING";

                return new ServiceResponse<FlightOrderResponseDto>
                {
                    Success = true,
                    Message = "Flight booked successfully!",
                    Data = new FlightOrderResponseDto
                    {
                        DuffelOrderId = data.GetProperty("id").GetString(),
                        PNR = pnr,
                        BookingStatus = data.GetProperty("booking_status").GetString()

                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<FlightOrderResponseDto>
                {
                    Success = false,
                    Message = $"An error occurred while booking the flight: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResponse<CheckoutResponseDto>> CreateCheckoutSessionAsync(CheckoutRequestDto request)
        {
            try
            {

                var offerResponse = await GetFlightDetailsAsync(request.OfferId); ;

                if (!offerResponse.Success || offerResponse.Data == null)
                {
                    return new ServiceResponse<CheckoutResponseDto>
                    {
                        Success = false,
                        Message = "Invalid Offer ID or the flight has already sold out."
                    };
                }

                // Extract the authentic, server-validated price
                decimal realPrice = offerResponse.Data.TotalPrice;
                string realCurrency = offerResponse.Data.Currency;

                // 2. Stripe requires the price in the SMALLEST currency unit (cents)
                long amountInCents = (long)(realPrice * 100);

                // 3. Tell Stripe to prepare for a payment
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = realCurrency.ToLower(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "OfferId", request.OfferId },
                        { "UserId", request.UserId }
                    }
                };

                var service = new PaymentIntentService();
                PaymentIntent intent = await service.CreateAsync(options);

                // 4. Save the Pending Booking to your SQL Database using Ardalis
                var flightBooking = new FlightBooking
                {
                    UserId = request.UserId,
                    OfferId = request.OfferId,
                    TotalPrice = realPrice, // Save the REAL price
                    Currency = realCurrency, // Save the REAL currency
                    StripePaymentIntentId = intent.Id,
                    BookingStatus = FlightBookingStatus.PendingPayment,
                    PNR = "PEND_" + Guid.NewGuid().ToString().Substring(0, 5).ToUpper(),
                    PassengersJson = JsonSerializer.Serialize(request.Passengers)
                };

                await _bookingRepo.AddAsync(flightBooking);

                return new ServiceResponse<CheckoutResponseDto>
                {
                    Success = true,
                    Message = "Checkout session created successfully.",
                    Data = new CheckoutResponseDto
                    {
                        ClientSecret = intent.ClientSecret,
                        StripeIntentId = intent.Id
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<CheckoutResponseDto>
                {
                    Success = false,
                    Message = $"Payment setup failed: {ex.Message}"
                };
            }
        }
    }
}


