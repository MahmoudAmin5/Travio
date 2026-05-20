using System.Text.Json.Serialization;

namespace Travio.Core.Services.Hotelbeds.ApiModels
{
    // ========================================================================================
    // RAW HOTELBEDS API REQUEST/RESPONSE MODELS
    // These classes map 1:1 to the Hotelbeds APITUDE JSON schema.
    // They are internal to the service layer — external consumers use the DTOs.
    // ========================================================================================

    // ─── AVAILABILITY REQUEST ────────────────────────────────────────────────────

    /// <summary>Raw JSON body sent to POST /hotels</summary>
    public class HotelbedsAvailabilityRequest
    {
        [JsonPropertyName("stay")]
        public HotelbedsStay Stay { get; set; } = new();

        [JsonPropertyName("occupancies")]
        public List<HotelbedsOccupancy> Occupancies { get; set; } = new();

        [JsonPropertyName("destination")]
        public HotelbedsDestination? Destination { get; set; }

        [JsonPropertyName("hotels")]
        public HotelbedsHotelsFilter? Hotels { get; set; }

        [JsonPropertyName("filter")]
        public HotelbedsFilter? Filter { get; set; }
        [JsonPropertyName("geolocation")]
        public HotelbedsGeolocation? Geolocation { get; set; }
    }
    public class HotelbedsGeolocation
    {
        [JsonPropertyName("latitude")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal Longitude { get; set; }

        [JsonPropertyName("radius")]
        public int? Radius { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "km";
    }
    public class HotelbedsStay
    {
        [JsonPropertyName("checkIn")]
        public string CheckIn { get; set; } = string.Empty;

        [JsonPropertyName("checkOut")]
        public string CheckOut { get; set; } = string.Empty;
    }

    public class HotelbedsOccupancy
    {
        [JsonPropertyName("rooms")]
        public int Rooms { get; set; } = 1;

        [JsonPropertyName("adults")]
        public int Adults { get; set; } = 1;

        [JsonPropertyName("children")]
        public int Children { get; set; } = 0;

        [JsonPropertyName("paxes")]
        public List<HotelbedsPax>? Paxes { get; set; }
    }

    public class HotelbedsPax
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "AD";

        [JsonPropertyName("age")]
        public int? Age { get; set; }
    }

    public class HotelbedsDestination
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }

    public class HotelbedsHotelsFilter
    {
        [JsonPropertyName("hotel")]
        public List<int> Hotel { get; set; } = new();
    }

    public class HotelbedsFilter
    {
        [JsonPropertyName("maxHotels")]
        public int? MaxHotels { get; set; }

        [JsonPropertyName("minCategory")]
        public int? MinCategory { get; set; }

        [JsonPropertyName("maxCategory")]
        public int? MaxCategory { get; set; }
    }

    // ─── AVAILABILITY RESPONSE ───────────────────────────────────────────────────

    /// <summary>Raw JSON response from POST /hotels</summary>
    public class HotelbedsAvailabilityResponse
    {
        [JsonPropertyName("hotels")]
        public HotelbedsHotelsContainer? Hotels { get; set; }
    }

    public class HotelbedsHotelsContainer
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("hotels")]
        public List<HotelbedsHotel>? Hotels { get; set; }
    }

    public class HotelbedsHotel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("categoryCode")]
        public string? CategoryCode { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("destinationCode")]
        public string? DestinationCode { get; set; }

        [JsonPropertyName("destinationName")]
        public string? DestinationName { get; set; }

        [JsonPropertyName("latitude")]
        public string? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string? Longitude { get; set; }

        [JsonPropertyName("minRate")]
        public string? MinRate { get; set; }

        [JsonPropertyName("maxRate")]
        public string? MaxRate { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("rooms")]
        public List<HotelbedsRoom>? Rooms { get; set; }
    }

    public class HotelbedsRoom
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rates")]
        public List<HotelbedsRate>? Rates { get; set; }
    }

    public class HotelbedsRate
    {
        [JsonPropertyName("rateKey")]
        public string? RateKey { get; set; }

        [JsonPropertyName("rateClass")]
        public string? RateClass { get; set; }

        [JsonPropertyName("net")]
        public string? Net { get; set; }

        [JsonPropertyName("boardCode")]
        public string? BoardCode { get; set; }

        [JsonPropertyName("boardName")]
        public string? BoardName { get; set; }

        [JsonPropertyName("allotment")]
        public int Allotment { get; set; }

        [JsonPropertyName("cancellationPolicies")]
        public List<HotelbedsCancellationPolicy>? CancellationPolicies { get; set; }
    }

    public class HotelbedsCancellationPolicy
    {
        [JsonPropertyName("amount")]
        public string? Amount { get; set; }

        [JsonPropertyName("from")]
        public string? From { get; set; }
    }

    // ─── CHECKRATE REQUEST/RESPONSE ──────────────────────────────────────────────

    /// <summary>Raw JSON body sent to POST /checkrates</summary>
    public class HotelbedsCheckRateRequest
    {
        [JsonPropertyName("rooms")]
        public List<HotelbedsCheckRateRoom> Rooms { get; set; } = new();
    }

    public class HotelbedsCheckRateRoom
    {
        [JsonPropertyName("rateKey")]
        public string RateKey { get; set; } = string.Empty;
    }

    /// <summary>Raw JSON response from POST /checkrates</summary>
    public class HotelbedsCheckRateResponse
    {
        [JsonPropertyName("hotel")]
        public HotelbedsCheckRateHotel? Hotel { get; set; }
    }

    public class HotelbedsCheckRateHotel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("categoryCode")]
        public string? CategoryCode { get; set; }

        [JsonPropertyName("destinationCode")]
        public string? DestinationCode { get; set; }

        [JsonPropertyName("checkIn")]
        public string? CheckIn { get; set; }

        [JsonPropertyName("checkOut")]
        public string? CheckOut { get; set; }

        [JsonPropertyName("totalNet")]
        public string? TotalNet { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("rooms")]
        public List<HotelbedsRoom>? Rooms { get; set; }
    }

    // ─── BOOKING REQUEST/RESPONSE ────────────────────────────────────────────────

    /// <summary>Raw JSON body sent to POST /bookings</summary>
    public class HotelbedsBookingRequest
    {
        [JsonPropertyName("holder")]
        public HotelbedsHolder Holder { get; set; } = new();

        [JsonPropertyName("rooms")]
        public List<HotelbedsBookingRoom> Rooms { get; set; } = new();

        [JsonPropertyName("clientReference")]
        public string ClientReference { get; set; } = string.Empty;

        [JsonPropertyName("remark")]
        public string? Remark { get; set; }
    }

    public class HotelbedsHolder
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("surname")]
        public string Surname { get; set; } = string.Empty;
    }

    public class HotelbedsBookingRoom
    {
        [JsonPropertyName("rateKey")]
        public string RateKey { get; set; } = string.Empty;

        [JsonPropertyName("paxes")]
        public List<HotelbedsBookingPax> Paxes { get; set; } = new();
    }

    public class HotelbedsBookingPax
    {
        [JsonPropertyName("roomId")]
        public int RoomId { get; set; } = 1;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "AD";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("surname")]
        public string Surname { get; set; } = string.Empty;

        [JsonPropertyName("age")]
        public int? Age { get; set; }
    }

    /// <summary>Raw JSON response from POST /bookings and GET /bookings/{ref}</summary>
    public class HotelbedsBookingResponse
    {
        [JsonPropertyName("booking")]
        public HotelbedsBookingDetail? Booking { get; set; }
    }

    public class HotelbedsBookingDetail
    {
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("clientReference")]
        public string? ClientReference { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("creationDate")]
        public string? CreationDate { get; set; }

        [JsonPropertyName("totalNet")]
        public string? TotalNet { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("holder")]
        public HotelbedsHolder? Holder { get; set; }

        [JsonPropertyName("hotel")]
        public HotelbedsBookingHotelDetail? Hotel { get; set; }

        [JsonPropertyName("cancellationReference")]
        public string? CancellationReference { get; set; }
    }

    public class HotelbedsBookingHotelDetail
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("checkIn")]
        public string? CheckIn { get; set; }

        [JsonPropertyName("checkOut")]
        public string? CheckOut { get; set; }

        [JsonPropertyName("rooms")]
        public List<HotelbedsRoom>? Rooms { get; set; }
    }

    // ─── CONTENT API RESPONSE ────────────────────────────────────────────────────
    // Hotelbeds Content API (hotel-content-api/1.0/) for hotel details, images, facilities.
    // Uses the SAME authentication as the Booking API.

    /// <summary>Raw JSON response from GET /hotels (Content API)</summary>
    public class HotelbedsContentResponse
    {
        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("hotels")]
        public List<HotelbedsContentHotel>? Hotels { get; set; }
    }

    /// <summary>Full hotel content from the Content API — includes images, descriptions, facilities.</summary>
    public class HotelbedsContentHotel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public HotelbedsContentValue? Name { get; set; }

        [JsonPropertyName("description")]
        public HotelbedsContentValue? Description { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("stateCode")]
        public string? StateCode { get; set; }

        [JsonPropertyName("destinationCode")]
        public string? DestinationCode { get; set; }

        [JsonPropertyName("coordinates")]
        public HotelbedsCoordinates? Coordinates { get; set; }

        [JsonPropertyName("categoryCode")]
        public string? CategoryCode { get; set; }

        [JsonPropertyName("categoryGroupCode")]
        public string? CategoryGroupCode { get; set; }

        [JsonPropertyName("accommodationType")]
        public HotelbedsAccommodationType? AccommodationType { get; set; }

        [JsonPropertyName("address")]
        public HotelbedsContentValue? Address { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("city")]
        public HotelbedsContentValue? City { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("web")]
        public string? Web { get; set; }

        [JsonPropertyName("phones")]
        public List<HotelbedsPhone>? Phones { get; set; }

        [JsonPropertyName("facilities")]
        public List<HotelbedsContentFacility>? Facilities { get; set; }

        [JsonPropertyName("images")]
        public List<HotelbedsContentImage>? Images { get; set; }

        [JsonPropertyName("rooms")]
        public List<HotelbedsContentRoom>? Rooms { get; set; }
    }

    public class HotelbedsContentRoom
    {
        [JsonPropertyName("roomCode")]
        public string? RoomCode { get; set; }

        [JsonPropertyName("characteristicCode")]
        public string? CharacteristicCode { get; set; }

        [JsonPropertyName("roomType")]
        public string? RoomType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("roomFacilities")]
        public List<HotelbedsRoomFacility>? RoomFacilities { get; set; }
    }

    /// <summary>Facility reference inside a Content API room object.</summary>
    public class HotelbedsRoomFacility
    {
        [JsonPropertyName("facilityCode")]
        public int FacilityCode { get; set; }

        [JsonPropertyName("facilityGroupCode")]
        public int FacilityGroupCode { get; set; }

        [JsonPropertyName("indYesOrNo")]
        public bool? IndYesOrNo { get; set; }

        [JsonPropertyName("number")]
        public int? Number { get; set; }
    }

    /// <summary>Generic content value wrapper used by Hotelbeds Content API.</summary>
    public class HotelbedsContentValue
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class HotelbedsCoordinates
    {
        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }
    }

    public class HotelbedsContentCategory
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("description")]
        public HotelbedsContentValue? Description { get; set; }
    }

    public class HotelbedsAccommodationType
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("typeDescription")]
        public string? TypeDescription { get; set; }
    }

    public class HotelbedsPhone
    {
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("phoneType")]
        public string? PhoneType { get; set; }
    }

    public class HotelbedsContentFacility
    {
        [JsonPropertyName("facilityCode")]
        public int FacilityCode { get; set; }

        [JsonPropertyName("facilityGroupCode")]
        public int FacilityGroupCode { get; set; }

        [JsonPropertyName("description")]
        public HotelbedsContentValue? Description { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }

    public class HotelbedsContentImage
    {
        [JsonPropertyName("imageTypeCode")]
        public string? ImageTypeCode { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("visualOrder")]
        public int VisualOrder { get; set; }

        [JsonPropertyName("roomCode")]
        public string? RoomCode { get; set; }

        [JsonPropertyName("characteristicCode")]
        public string? CharacteristicCode { get; set; }
    }

    // ─── ERROR RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Hotelbeds API error envelope.</summary>
    public class HotelbedsErrorResponse
    {
        [JsonPropertyName("error")]
        public HotelbedsError? Error { get; set; }
    }

    public class HotelbedsError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    // ─── CONTENT API: DESTINATIONS & FACILITIES ────────────────────────────────

    public class HotelbedsLocationsResponse
    {
        [JsonPropertyName("destinations")]
        public List<HotelbedsContentDestination>? Destinations { get; set; }
    }

    public class HotelbedsContentDestination
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public HotelbedsContentValue? Name { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
    }

    public class HotelbedsFacilitiesResponse
    {
        [JsonPropertyName("facilities")]
        public List<HotelbedsContentFacilityData>? Facilities { get; set; }
    }

    public class HotelbedsContentFacilityData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("facilityGroupCode")]
        public int FacilityGroupCode { get; set; }

        [JsonPropertyName("description")]
        public HotelbedsContentValue? Description { get; set; }
    }
}
