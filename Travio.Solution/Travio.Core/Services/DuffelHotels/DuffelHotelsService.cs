using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelHotels;
using Travio.Core.DTOs.DuffelHotelsDTOs;
using Travio.Core.DTOs.DuffelHotelsDTOs.Requests;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Services.DuffelHotels
{
    public class DuffelHotelsService : IDuffelHotelsService
    {
        private readonly HttpClient _httpClient;

        public DuffelHotelsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<ServiceResponse<List<HotelSearchResultDto>>> SearchHotelsAsync(HotelSearchRequestDto request)
        {
            try
            {
                // 1. Build the precise JSON Payload Duffel requires
                var payload = new
                {
                    data = new
                    {
                        rooms = request.Rooms,
                        check_in_date = request.CheckInDate,
                        check_out_date = request.CheckOutDate,
                        location = new
                        {
                            radius = request.RadiusKm,
                            geographic_coordinates = new
                            {
                                latitude = request.Latitude,
                                longitude = request.Longitude
                            }
                        },
                        // Duffel requires an array of guest objects. We generate this dynamically!
                        guests = Enumerable.Repeat(new { type = "adult" }, request.Adults).ToArray()
                    }
                };

                // 2. Serialize the object into JSON
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                // 3. MAGIC FIX: Send a POST request, not a GET request!
                var response = await _httpClient.PostAsync("stays/search", jsonContent);

                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ServiceResponse<List<HotelSearchResultDto>>
                    {
                        Success = false,
                        Message = $"Duffel API Error: {jsonString}"
                    };
                }

                // 4. Parse the JSON response
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var resultsArray = jsonDoc.RootElement.GetProperty("data").GetProperty("results").EnumerateArray();

                var hotels = new List<HotelSearchResultDto>();

                foreach (var result in resultsArray)
                {
                    var property = result.GetProperty("property");

                    var photos = property.GetProperty("photos").EnumerateArray().ToList();
                    string mainImage = photos.Any() ? photos.First().GetProperty("url").GetString() : "https://placehold.co/600x400?text=No+Image";

                    double? rating = property.GetProperty("rating").ValueKind != JsonValueKind.Null
                                     ? property.GetProperty("rating").GetDouble()
                                     : null;

                    hotels.Add(new HotelSearchResultDto
                    {
                        PropertyId = property.GetProperty("id").GetString(),
                        HotelName = property.GetProperty("name").GetString(),
                        Rating = rating,
                        Latitude = property.GetProperty("latitude").GetDouble(),
                        Longitude = property.GetProperty("longitude").GetDouble(),
                        MainImageUrl = mainImage,
                        StartingPrice = decimal.Parse(result.GetProperty("cheapest_rate_total_amount").GetString()),
                        Currency = result.GetProperty("cheapest_rate_currency").GetString()
                    });
                }

                return new ServiceResponse<List<HotelSearchResultDto>>
                {
                    Success = true,
                    Message = $"Found {hotels.Count} hotels.",
                    Data = hotels.OrderBy(h => h.StartingPrice).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<HotelSearchResultDto>>
                {
                    Success = false,
                    Message = $"An error occurred while searching for hotels: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResponse<HotelDetailsDto>> GetHotelDetailsAsync(string searchResultId)
        {
            try
            {
                // 1. Hit the Duffel API with the ID from Step 1
                var response = await _httpClient.GetAsync($"stays/search_results/{searchResultId}");
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ServiceResponse<HotelDetailsDto>
                    {
                        Success = false,
                        Message = "Could not retrieve hotel details. The search may have expired."
                    };
                }

                using var jsonDoc = JsonDocument.Parse(jsonString);
                var data = jsonDoc.RootElement.GetProperty("data");
                var property = data.GetProperty("property");

               
                var photos = new List<string>();
                if (property.GetProperty("photos").ValueKind == JsonValueKind.Array)
                {
                    foreach (var photo in property.GetProperty("photos").EnumerateArray())
                    {
                        photos.Add(photo.GetProperty("url").GetString());
                    }
                }

               
                var amenities = new List<string>();
                if (property.GetProperty("amenities").ValueKind == JsonValueKind.Array)
                {
                    foreach (var amenity in property.GetProperty("amenities").EnumerateArray())
                    {
                        
                        if (amenity.TryGetProperty("description", out var desc))
                        {
                            amenities.Add(desc.GetString());
                        }
                    }
                }

                
                var rooms = new List<RoomRateDto>();
                foreach (var rate in data.GetProperty("rates").EnumerateArray())
                {
                    rooms.Add(new RoomRateDto
                    {
                        RateId = rate.GetProperty("id").GetString(),
                        RoomName = rate.GetProperty("room_name").GetString(),
                        BoardType = rate.GetProperty("board_type").GetString(),
                        Price = decimal.Parse(rate.GetProperty("total_amount").GetString()),
                        Currency = rate.GetProperty("currency").GetString()
                    });
                }

                
                var details = new HotelDetailsDto
                {
                    PropertyId = property.GetProperty("id").GetString(),
                    HotelName = property.GetProperty("name").GetString(),
                    Rating = property.GetProperty("rating").ValueKind != JsonValueKind.Null ? property.GetProperty("rating").GetDouble() : null,
                    Photos = photos,
                    Amenities = amenities,
                    AvailableRooms = rooms.OrderBy(r => r.Price).ToList() // Always sort cheapest first!
                };

                return new ServiceResponse<HotelDetailsDto>
                {
                    Success = true,
                    Message = "Hotel details retrieved successfully.",
                    Data = details
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<HotelDetailsDto>
                {
                    Success = false,
                    Message = $"Error parsing hotel details: {ex.Message}"
                };
            }
        }

    }
}
