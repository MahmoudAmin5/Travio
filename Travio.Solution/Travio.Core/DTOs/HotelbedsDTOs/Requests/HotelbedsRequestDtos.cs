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
    // Supports multi-room carts: the mobile app sends ALL selected rooms for combined validation.
    // ========================================================================================

    /// <summary>
    /// Request DTO for confirming rates via the Hotelbeds CheckRate endpoint.
    /// 
    /// MULTI-ROOM SUPPORT: Accepts a list of rooms, each with its own RateKey.
    /// Hotelbeds treats this as a "shopping cart" and returns the combined TotalNet
    /// across all rooms. This is critical for mixed-rate bookings (e.g., Standard + Suite).
    /// 
    /// WHERE IT COMES FROM: The mobile app collects the RateKey from each room the user
    /// selected in the search/details response, then sends them as a list here.
    /// </summary>
    public class HotelCheckRateRequestDto
    {
        /// <summary>
        /// One entry per room in the user's cart. Each room carries its own RateKey.
        /// Minimum 1 room. For single-room bookings, the list has one element.
        /// 
        /// WHERE IT COMES FROM: The mobile app builds this list from the RateKeys
        /// the user selected on the hotel details / room selection screen.
        /// </summary>
        public required List<CheckRateRoomDto> Rooms { get; set; }
    }

    /// <summary>
    /// A single room in the CheckRate validation request.
    /// </summary>
    public class CheckRateRoomDto
    {
        /// <summary>
        /// The unique rate key for this room, obtained from the availability search.
        /// Each key encodes: hotel, room type, board, dates, and wholesale price.
        /// IMPORTANT: Rate keys expire within minutes — call CheckRate promptly.
        /// </summary>
        public required string RateKey { get; set; }
    }

    // ========================================================================================
    // STEP 3: BOOKING — Execute the final reservation.
    // ========================================================================================

    /// <summary>
    /// Request DTO for creating a hotel booking via the Hotelbeds Booking endpoint.
    /// Used at checkout time: the mobile app sends this payload to POST /api/Hotels/checkout.
    /// 
    /// MULTI-ROOM CART MODEL: There is NO root-level RateKey.
    /// Each room in the Rooms array carries its own RateKey. This supports mixed-rate
    /// carts where the user books different room types (e.g., Standard + Suite) in one transaction.
    /// 
    /// WHERE THESE FIELDS COME FROM:
    ///   - HolderFirstName/LastName → the user types this in the checkout form
    ///   - Remark → optional text the user types (e.g., "Late check-in")
    ///   - Rooms[].RateKey → from the Availability/Details response (each RateDto has a RateKey)
    ///   - Rooms[].Paxes → the user fills in guest names for each room from the checkout form
    /// </summary>
    public class HotelBookingRequestDto
    {
        /// <summary>
        /// First name of the booking holder — the person legally responsible for the reservation.
        /// WHERE IT COMES FROM: The user types this in the checkout form.
        /// </summary>
        public required string HolderFirstName { get; set; }

        /// <summary>
        /// Last name of the booking holder.
        /// WHERE IT COMES FROM: The user types this in the checkout form.
        /// </summary>
        public required string HolderLastName { get; set; }

        /// <summary>
        /// Optional special request for the hotel (e.g., "Late check-in at 11 PM", "Honeymoon couple").
        /// WHERE IT COMES FROM: Optional text field in the checkout form.
        /// Not all hotels honor remarks, but they are forwarded via Hotelbeds.
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Guest details for each room being booked. EACH ROOM HAS ITS OWN RATE KEY.
        /// This is the authoritative source for rate keys — there is no root-level RateKey.
        /// 
        /// WHERE IT COMES FROM: The checkout form has a "Guests" section for each room.
        /// The user fills in the name and type (adult/child) of each guest.
        /// The RateKey for each room comes from the Availability/Details search response.
        /// </summary>
        public required List<BookingRoomDto> Rooms { get; set; }
    }

    /// <summary>
    /// Guest information and rate selection for a single room in the booking.
    /// This is the AUTHORITATIVE source of the rate key for this room.
    /// </summary>
    public class BookingRoomDto
    {
        /// <summary>
        /// The rate key for this specific room.
        /// IMPORTANT: This is the ONLY place rate keys live — there is no root-level key.
        /// Each room can have a different rate (e.g., Room 1 = Standard BB, Room 2 = Suite RO).
        /// 
        /// WHERE IT COMES FROM: The mobile app gets this from the "Rates" array in the
        /// availability/details response. Each RateDto has a RateKey property.
        /// </summary>
        public required string RateKey { get; set; }

        /// <summary>
        /// List of guests (paxes) occupying this room.
        /// Must include at least one adult (Type = "AD").
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
