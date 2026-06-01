namespace Travio.Core.DTOs.HotelbedsDTOs.Responses
{
    // ========================================================================================
    // SHARED DTOs — Image, Facility, Phone (used across search + details)
    // ========================================================================================

    /// <summary>
    /// Hotel image with full URL, type code, and display order.
    /// </summary>
    public class HotelImageDto
    {
        /// <summary>Full URL to the image (e.g., "http://photos.hotelbeds.com/giata/bigger/12/...").</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Image type code: "GEN" (general), "ROM" (room), "RES" (restaurant), "PLA" (plans), etc.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Display order for the image (lower = shown first).</summary>
        public int Order { get; set; }
    }

    /// <summary>Hotel facility/amenity.</summary>
    public class HotelFacilityDto
    {
        /// <summary>Hotelbeds facility code.</summary>
        public int Code { get; set; }

        /// <summary>Facility group code (e.g., 70 = General, 60 = Room).</summary>
        public int GroupCode { get; set; }

        /// <summary>Human-readable description (e.g., "Free Wi-Fi", "Swimming pool").</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>Hotel phone contact.</summary>
    public class HotelPhoneDto
    {
        /// <summary>Phone type (e.g., "PHONEHOTEL", "PHONEBOOKING", "FAXNUMBER").</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Phone number.</summary>
        public string Number { get; set; } = string.Empty;
    }

    // ========================================================================================
    // SEARCH RESPONSE — Basic hotel cards with thumbnail images for listing pages.
    // ========================================================================================

    /// <summary>
    /// Response from the hotel search endpoint.
    /// Returns hotel cards with basic info + thumbnail images for listing display.
    /// </summary>
    public class HotelAvailabilityResponseDto
    {
        /// <summary>List of available hotels matching the search criteria.</summary>
        public List<HotelSearchResultDto> Hotels { get; set; } = new();

        /// <summary>Total number of hotels found.</summary>
        public int TotalHotels { get; set; }
    }

    /// <summary>
    /// A hotel card for the search results — includes thumbnail + basic pricing.
    /// </summary>
    public class HotelSearchResultDto
    {
        /// <summary>Hotelbeds numeric hotel code.</summary>
        public int Code { get; set; }

        /// <summary>Hotel name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Star category code (e.g., "4EST" = 4-star).</summary>
        public string CategoryCode { get; set; } = string.Empty;

        /// <summary>Human-readable category name (e.g., "4 STARS").</summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Hotelbeds destination code.</summary>
        public string DestinationCode { get; set; } = string.Empty;

        /// <summary>Destination/zone name.</summary>
        public string DestinationName { get; set; } = string.Empty;

        /// <summary>Latitude of the hotel.</summary>
        public decimal? Latitude { get; set; }

        /// <summary>Longitude of the hotel.</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Minimum price across all available rooms (includes 15% corporate markup).</summary>
        public decimal MinRate { get; set; }

        /// <summary>Maximum price across all available rooms (includes 15% corporate markup).</summary>
        public decimal MaxRate { get; set; }

        /// <summary>ISO 4217 currency code (e.g., "EUR", "USD").</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Primary thumbnail image URL for the hotel card. Null if no images available.</summary>
        public string? ThumbnailImage { get; set; }

        /// <summary>Hotel images (up to 5 images in search results for carousel previews).</summary>
        public List<HotelImageDto> Images { get; set; } = new();
    }

    // ========================================================================================
    // HOTEL DETAILS RESPONSE — Full hotel page with all images, description, rooms/rates.
    // ========================================================================================

    /// <summary>
    /// Full hotel details for the hotel page — combines Content API (static) + Availability API (live).
    /// </summary>
    public class HotelDetailResponseDto
    {
        /// <summary>Hotelbeds hotel code.</summary>
        public int Code { get; set; }

        /// <summary>Hotel name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Full hotel description from the Content API.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Star category code.</summary>
        public string CategoryCode { get; set; } = string.Empty;

        /// <summary>Human-readable category name.</summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Accommodation type code (e.g., "HOTEL", "HOSTAL", "APART").</summary>
        public string AccommodationType { get; set; } = string.Empty;

        /// <summary>Street address.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Postal/ZIP code.</summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>City name.</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>ISO country code (e.g., "ES", "US").</summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>Destination code.</summary>
        public string DestinationCode { get; set; } = string.Empty;

        /// <summary>Latitude.</summary>
        public decimal? Latitude { get; set; }

        /// <summary>Longitude.</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Hotel email address.</summary>
        public string? Email { get; set; }

        /// <summary>Hotel website URL.</summary>
        public string? Web { get; set; }

        /// <summary>Hotel phone contacts.</summary>
        public List<HotelPhoneDto> Phones { get; set; } = new();

        /// <summary>All hotel images (full gallery).</summary>
        public List<HotelImageDto> Images { get; set; } = new();

        /// <summary>Hotel facilities and amenities.</summary>
        public List<HotelFacilityDto> Facilities { get; set; } = new();

        /// <summary>
        /// Available rooms with rates for the requested dates.
        /// Null if no dates were provided (content-only request).
        /// </summary>
        public List<AvailableRoomDto>? Rooms { get; set; }

        /// <summary>Minimum rate from availability (null if no dates provided).</summary>
        public decimal? MinRate { get; set; }

        /// <summary>Maximum rate from availability (null if no dates provided).</summary>
        public decimal? MaxRate { get; set; }

        /// <summary>Currency for rates (null if no dates provided).</summary>
        public string? Currency { get; set; }
    }

    // ========================================================================================
    // ROOM & RATE DTOs — Used by both Search (in full mode) and Details endpoints.
    // ========================================================================================

    /// <summary>A room type with its available rate options.</summary>
    public class AvailableRoomDto
    {
        /// <summary>Hotelbeds room code.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Room type name (e.g., "Double Standard Room").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>HAB Images (Photos of the specific room).</summary>
        public List<HotelImageDto> Images { get; set; } = new();

        /// <summary>Room amenities/facilities (e.g., "Air Conditioning", "Bathtub", "Minibar").</summary>
        public List<string> RoomFacilities { get; set; } = new();

        /// <summary>Rate plans available for this room.</summary>
        public List<RateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// A specific rate option — contains the RateKey needed for CheckRate and Booking.
    /// </summary>
    public class RateDto
    {
        /// <summary>
        /// The unique rate key. CRITICAL for Step 2 (CheckRate) and Step 3 (Booking).
        /// Rate keys expire — act quickly.
        /// </summary>
        public string RateKey { get; set; } = string.Empty;

        /// <summary>Rate class: "NOR" (normal), "NRF" (non-refundable).</summary>
        public string RateClass { get; set; } = string.Empty;

        /// <summary>Net price for this rate.</summary>
        public decimal Price { get; set; }

        /// <summary>Board code: "RO" (Room Only), "BB" (Bed & Breakfast), "HB" (Half Board), etc.</summary>
        public string BoardCode { get; set; } = string.Empty;

        /// <summary>Human-readable board name.</summary>
        public string BoardName { get; set; } = string.Empty;

        /// <summary>Number of rooms available at this rate.</summary>
        public int Allotment { get; set; }

        /// <summary>Cancellation policies associated with this rate.</summary>
        public List<CancellationPolicyDto> CancellationPolicies { get; set; } = new();
    }

    /// <summary>Cancellation policy details.</summary>
    public class CancellationPolicyDto
    {
        /// <summary>Cancellation fee amount.</summary>
        public decimal Amount { get; set; }

        /// <summary>Date/time from which cancellation fees apply (ISO 8601).</summary>
        public string From { get; set; } = string.Empty;
    }

    // ========================================================================================
    // CHECKRATE RESPONSE — Confirmed rate details and cancellation policies.
    // ========================================================================================

    /// <summary>Response from the CheckRate endpoint with confirmed pricing.</summary>
    public class HotelCheckRateResponseDto
    {
        /// <summary>The confirmed hotel details.</summary>
        public CheckRateHotelDto? Hotel { get; set; }
    }

    /// <summary>Hotel details from CheckRate with confirmed pricing.</summary>
    public class CheckRateHotelDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string CheckOut { get; set; } = string.Empty;
        public List<AvailableRoomDto> Rooms { get; set; } = new();
    }

    // ========================================================================================
    // BOOKING RESPONSE — Confirmed booking with Hotelbeds reference.
    // ========================================================================================

    /// <summary>Response from the Booking endpoint with confirmation details.</summary>
    public class HotelBookingResponseDto
    {
        /// <summary>The Hotelbeds booking reference (e.g., "1-234567").</summary>
        public string BookingReference { get; set; } = string.Empty;

        /// <summary>Your client reference (echoed back).</summary>
        public string ClientReference { get; set; } = string.Empty;

        /// <summary>Booking status from Hotelbeds (e.g., "CONFIRMED").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Hotel details for the confirmed booking.</summary>
        public BookingHotelDto? Hotel { get; set; }

        /// <summary>Total price.</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>Currency.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>UTC timestamp of creation.</summary>
        public string CreationDate { get; set; } = string.Empty;
    }

    /// <summary>Hotel summary inside booking confirmation.</summary>
    public class BookingHotelDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string CheckOut { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }

    // ========================================================================================
    // BOOKING MANAGEMENT — User bookings, booking detail, cancellation.
    // ========================================================================================

    /// <summary>
    /// A user's hotel booking from the database — used for the "My Bookings" listing.
    /// </summary>
    public class UserHotelBookingDto
    {
        public Guid Id { get; set; }
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string CheckOut { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? HotelbedsReference { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public int RoomCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Full booking detail retrieved live from the Hotelbeds API (GET /bookings/{ref}).
    /// </summary>
    public class BookingDetailResponseDto
    {
        /// <summary>Hotelbeds booking reference.</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>Client reference.</summary>
        public string ClientReference { get; set; } = string.Empty;

        /// <summary>Booking status (e.g., "CONFIRMED", "CANCELLED").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Creation date.</summary>
        public string CreationDate { get; set; } = string.Empty;

        /// <summary>Booking holder name.</summary>
        public string HolderName { get; set; } = string.Empty;

        /// <summary>Total net price.</summary>
        public decimal TotalNet { get; set; }

        /// <summary>Currency.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Hotel details.</summary>
        public BookingHotelDto? Hotel { get; set; }

        /// <summary>Cancellation reference (null if not cancelled).</summary>
        public string? CancellationReference { get; set; }
    }

    /// <summary>
    /// Response from a booking cancellation (DELETE /bookings/{ref}).
    /// </summary>
    public class BookingCancellationResponseDto
    {
        /// <summary>Hotelbeds booking reference.</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>New status after cancellation (should be "CANCELLED").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>The cancellation reference issued by Hotelbeds.</summary>
        public string CancellationReference { get; set; } = string.Empty;
    }

    // ========================================================================================
    // CHECKOUT RESPONSE — Returned by POST /checkout after Stripe PaymentIntent creation.
    // ========================================================================================

    /// <summary>
    /// Response from the checkout initialization endpoint.
    /// Contains the Stripe client secret for the mobile app to render the payment sheet.
    /// </summary>
    public class CheckoutResponseDto
    {
        /// <summary>The Stripe client secret for the PaymentIntent. Pass this to the Stripe SDK.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Our internal booking ID (persisted as PendingPayment in the DB).</summary>
        public Guid BookingId { get; set; }

        /// <summary>The validated total price (15% markup + currency conversion applied).</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>ISO 4217 currency code (e.g., "USD").</summary>
        public string Currency { get; set; } = "USD";
    }
}
