namespace Travio.Core.DTOs.HotelbedsDTOs.Requests
{
    // ========================================================================================
    // STEP 1: AVAILABILITY — Search for available hotels by destination, dates, and occupancies.
    // ========================================================================================

    /// <summary>
    /// Request DTO for searching hotel availability via Hotelbeds APITUDE.
    /// Maps to the POST /hotels endpoint.
    /// </summary>
    public class HotelAvailabilityRequestDto
    {
        /// <summary>
        /// The check-in date in "yyyy-MM-dd" format.
        /// Must be today or a future date.
        /// </summary>
        public required string CheckIn { get; set; }

        /// <summary>
        /// The check-out date in "yyyy-MM-dd" format.
        /// Must be after CheckIn.
        /// </summary>
        public required string CheckOut { get; set; }

        /// <summary>
        /// The Hotelbeds destination code (e.g., "MCT" for Muscat, "PMI" for Palma de Mallorca).
        /// Either DestinationCode or HotelCodes should be provided, not both.
        /// </summary>
        public string? DestinationCode { get; set; }
        public string? DestinationName { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? RadiusInKm { get; set; } = 20;

        /// <summary>
        /// Specific hotel codes to search (optional — alternative to DestinationCode).
        /// When provided, limits the search to these specific hotels.
        /// </summary>
        public List<int>? HotelCodes { get; set; }

        /// <summary>
        /// Room occupancy configurations. At least one room is required.
        /// Each occupancy defines the number of adults and optional children ages.
        /// </summary>
        public required List<OccupancyDto> Occupancies { get; set; }

        /// <summary>
        /// Maximum number of hotels to return (default: 20, max: 2000 per Hotelbeds API).
        /// </summary>
        public int MaxHotels { get; set; } = 20;

        /// <summary>
        /// Minimum star rating filter (1-5). Null means no filter.
        /// </summary>
        public int? MinCategory { get; set; }

        /// <summary>
        /// Maximum star rating filter (1-5). Null means no filter.
        /// </summary>
        public int? MaxCategory { get; set; }
    }

    /// <summary>
    /// Defines the occupancy for a single room.
    /// </summary>
    public class OccupancyDto
    {
        /// <summary>Number of adult guests in this room (minimum 1).</summary>
        public int Adults { get; set; } = 1;

        /// <summary>Number of child guests in this room (0 if none).</summary>
        public int Children { get; set; } = 0;

        /// <summary>
        /// Ages of each child guest. Required when Children > 0.
        /// Hotelbeds requires child ages for accurate pricing.
        /// </summary>
        public List<int>? ChildrenAges { get; set; }
    }

    // ========================================================================================
    // STEP 2: CHECKRATE — Validate a specific rate before booking.
    // ========================================================================================

    /// <summary>
    /// Request DTO for confirming a rate via the Hotelbeds CheckRate endpoint.
    /// The RateKey is obtained from the availability search response.
    /// </summary>
    public class HotelCheckRateRequestDto
    {
        /// <summary>
        /// The unique rate key returned by the availability search.
        /// This key encodes hotel, room, board, and pricing info.
        /// IMPORTANT: Rate keys expire — call CheckRate promptly after availability.
        /// </summary>
        public required string RateKey { get; set; }
    }

    // ========================================================================================
    // STEP 3: BOOKING — Execute the final reservation.
    // ========================================================================================

    /// <summary>
    /// Request DTO for creating a hotel booking via the Hotelbeds Booking endpoint.
    /// </summary>
    public class HotelBookingRequestDto
    {
        /// <summary>
        /// The rate key confirmed via CheckRate. This is required for booking.
        /// </summary>
        public required string RateKey { get; set; }

        /// <summary>
        /// The booking holder's first name (as it appears on identification).
        /// </summary>
        public required string HolderFirstName { get; set; }

        /// <summary>
        /// The booking holder's last name (as it appears on identification).
        /// </summary>
        public required string HolderLastName { get; set; }

        /// <summary>
        /// Client reference for this booking (your internal tracking ID).
        /// Max 20 characters. Must be unique.
        /// </summary>
        public string? ClientReference { get; set; }

        /// <summary>
        /// Optional special remark for the hotel (e.g., "Late check-in").
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Guest details for each room being booked.
        /// Must match the number of rooms from the rate key.
        /// </summary>
        public required List<BookingRoomDto> Rooms { get; set; }
    }

    /// <summary>
    /// Guest information for a single room in the booking request.
    /// </summary>
    public class BookingRoomDto
    {
        /// <summary>
        /// The rate key for this specific room (same as the parent RateKey for single-room bookings).
        /// </summary>
        public required string RateKey { get; set; }

        /// <summary>
        /// List of guests (paxes) occupying this room.
        /// Must include at least one adult.
        /// </summary>
        public required List<BookingPaxDto> Paxes { get; set; }
    }

    /// <summary>
    /// Individual guest (pax) details for the booking.
    /// </summary>
    public class BookingPaxDto
    {
        /// <summary>Room number this pax is assigned to (1-based).</summary>
        public int RoomId { get; set; } = 1;

        /// <summary>Guest type: "AD" for adult, "CH" for child.</summary>
        public required string Type { get; set; }

        /// <summary>Guest first name.</summary>
        public required string Name { get; set; }

        /// <summary>Guest last name / surname.</summary>
        public required string Surname { get; set; }

        /// <summary>Age of the guest. Required for children ("CH" type).</summary>
        public int? Age { get; set; }
    }

    // ========================================================================================
    // HOTEL DETAILS — Query parameters for the GET /hotels/{hotelCode}/details endpoint.
    // ========================================================================================

    /// <summary>
    /// Query parameters for retrieving full hotel details.
    /// Dates and occupancy are optional — if provided, live availability (rooms/rates) is included.
    /// If omitted, only static content (images, description, facilities) is returned.
    /// </summary>
    public class HotelDetailsQueryDto
    {
        /// <summary>Check-in date in "yyyy-MM-dd" format (optional — enables room/rate availability).</summary>
        public string? CheckIn { get; set; }

        /// <summary>Check-out date in "yyyy-MM-dd" format (optional — enables room/rate availability).</summary>
        public string? CheckOut { get; set; }

        /// <summary>Number of adults per room (default: 2). Used for availability lookup.</summary>
        public int Adults { get; set; } = 2;

        /// <summary>Number of children per room (default: 0).</summary>
        public int Children { get; set; } = 0;

        /// <summary>Comma-separated child ages (e.g., "8,5"). Required when Children > 0.</summary>
        public string? ChildrenAges { get; set; }
    }
}
