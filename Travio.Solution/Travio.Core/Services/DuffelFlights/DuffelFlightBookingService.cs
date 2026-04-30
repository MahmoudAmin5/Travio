using Duffel.ApiClient;
using Duffel.ApiClient.Models.Requests;
using Duffel.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.GenericResponse;
using System.Text.Json;

namespace Travio.Core.Services.DuffelFlights
{
    public class DuffelFlightBookingService : IDuffelFlightBookingService
    {
        private readonly HttpClient _httpClient;

        // Inject the standard HttpClient we just configured in Program.cs
        public DuffelFlightBookingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                        mappedSegments.Add(new FlightSegmentDto
                        {
                            Origin = seg.GetProperty("origin").GetProperty("iata_code").GetString(),
                            Destination = seg.GetProperty("destination").GetProperty("iata_code").GetString(),
                            DepartureTime = seg.GetProperty("departing_at").GetDateTime(),
                            ArrivalTime = seg.GetProperty("arriving_at").GetDateTime(),
                            AirlineName = seg.GetProperty("operating_carrier").GetProperty("name").GetString(),
                            FlightNumber = seg.GetProperty("operating_carrier_flight_number").GetString()
                        });
                    }
                    flights.Add(new FlightSearchResponseDto
                    {
                        OfferId = offer.GetProperty("id").GetString(),
                        TotalOrigin = firstSlice.GetProperty("origin").GetProperty("iata_code").GetString(),
                        TotalDestination = firstSlice.GetProperty("destination").GetProperty("iata_code").GetString(),
                        TotalPrice = decimal.Parse(offer.GetProperty("total_amount").GetString()),
                        Currency = offer.GetProperty("total_currency").GetString(),

                        Stops = mappedSegments.Count - 1,
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
          
                var popularRoutes = new List<(string Name, string Dest, string ImageUrl)>
        {
            ("Paris", "CDG", "https://images.unsplash.com/photo-1502602898657-3e9076113192?auto=format&fit=crop&w=800&q=80"),
            ("Dubai", "DXB", "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?auto=format&fit=crop&w=800&q=80"),
            ("London", "LHR", "https://images.unsplash.com/photo-1513635269975-59693e2482d5?auto=format&fit=crop&w=800&q=80")
        };

               
                var futureDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

               
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

                  
                    var response = await SearchFlightsAsync(request);

               
                    if (response.Success && response.Data != null && response.Data.Any())
                    {
                        
                        var cheapestFlight = response.Data.First();

                        return new TopFlightOfferDto
                        {
                            DestinationName = route.Name,
                            Origin = "CAI",
                            Destination = route.Dest,
                            TravelDate = futureDate,
                            ImageUrl = route.ImageUrl,
                            CheapestPrice = cheapestFlight.TotalPrice,
                            Currency = cheapestFlight.Currency,
                            AirlineName = cheapestFlight.Segments.FirstOrDefault()?.AirlineName ?? "Multiple Airlines",
                            OfferId = cheapestFlight.OfferId
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
                        DepartureTime = segment.GetProperty("departing_at").GetString(),

                        DestinationAirport = segment.GetProperty("destination").GetProperty("iata_code").GetString(),
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
    }
}


