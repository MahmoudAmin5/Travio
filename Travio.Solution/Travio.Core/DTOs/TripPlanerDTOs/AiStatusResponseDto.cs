using System.Text.Json.Serialization;

namespace Travio.Core.DTOs.TripPlanerDTOs;

public class AiStatusResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ItineraryData? Data { get; set; }
}

public class ItineraryData
{
    [JsonPropertyName("city_name")]
    public string? CityName { get; set; }

    [JsonPropertyName("city_hero_image")]
    public string? CityHeroImage { get; set; }

    [JsonPropertyName("recommended_hotels")]
    public List<RecommendedHotel>? RecommendedHotels { get; set; }

    [JsonPropertyName("itinerary")]
    public List<DailyPlan>? Itinerary { get; set; }
}

public class RecommendedHotel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("featured_image")]
    public string? FeaturedImage { get; set; }

    [JsonPropertyName("coordinates")]
    public Coordinates? Coordinates { get; set; }
}


public class DailyPlan
{
    [JsonPropertyName("day")]
    public int? Day { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("activities")]
    public List<Activity>? Activities { get; set; }
}

public class Activity
{
    [JsonPropertyName("activity_type")]
    public string? ActivityType { get; set; }

    [JsonPropertyName("place_name")]
    public string? PlaceName { get; set; }

    [JsonPropertyName("suggested_time")]
    public string? SuggestedTime { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("featured_image")]
    public string? FeaturedImage { get; set; }

    [JsonPropertyName("coordinates")]
    public Coordinates? Coordinates { get; set; }
}

public class Coordinates
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}