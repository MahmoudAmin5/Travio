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
                // 1. Build the JSON payload exactly how Duffel's docs ask for it
                var payload = new
                {
                    data = new
                    {
                        cabin_class = "economy",
                        passengers = Enumerable.Repeat(new { type = "adult" }, request.NumberOfAdults).ToList(),
                        slices = new[]
                        {
                            new
                            {
                                origin = request.Origin,
                                destination = request.Destination,
                                departure_date = request.DepartureDate
                            }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // 2. Make the POST request to the live Duffel API
                // Adding ?return_offers=true tells Duffel to send the actual flights back instantly
                var response = await _httpClient.PostAsync("air/offer_requests?return_offers=true", content);

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // If Duffel rejects it, we print their EXACT error message to the screen!
                    return new ServiceResponse<List<FlightSearchResponseDto>>
                    {
                        Success = false,
                        Message = $"Duffel API Error: {responseString}"
                    };
                }

                // 3. Parse the massive JSON response
                using var jsonDoc = JsonDocument.Parse(responseString);
                var offers = jsonDoc.RootElement.GetProperty("data").GetProperty("offers").EnumerateArray();

                var flights = new List<FlightSearchResponseDto>();

                foreach (var offer in offers)
                {
                    var slices = offer.GetProperty("slices").EnumerateArray();
                    var firstSlice = slices.FirstOrDefault();
                    var firstSegment = firstSlice.GetProperty("segments").EnumerateArray().FirstOrDefault();

                    flights.Add(new FlightSearchResponseDto
                    {
                        OfferId = offer.GetProperty("id").GetString(),
                        AirlineName = offer.GetProperty("owner").GetProperty("name").GetString(),
                        Origin = firstSlice.GetProperty("origin").GetProperty("iata_code").GetString(),
                        Destination = firstSlice.GetProperty("destination").GetProperty("iata_code").GetString(),
                        DepartureTime = firstSegment.GetProperty("departing_at").GetDateTime(),
                        ArrivalTime = firstSegment.GetProperty("arriving_at").GetDateTime(),
                        // Parse the string amount into a decimal
                        TotalPrice = decimal.Parse(offer.GetProperty("total_amount").GetString()),
                        Currency = offer.GetProperty("total_currency").GetString()
                    });
                }

                // 4. Return cheapest flights first!
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
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }
    }
    
}
