using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.HotelbedsDTOs.Requests;
using Travio.Core.DTOs.HotelbedsDTOs.Responses;
using Travio.Core.Domain.Entities.Hotelbeds;

namespace Travio.Core.Contracts.Services.Hotelbeds
{
    /// <summary>
    /// Defines the contract for the Hotelbeds APITUDE integration service.
    ///
    /// Endpoints:
    ///   Discovery:
    ///     1. SearchAvailability — Search hotels with basic info + images.
    ///     2. GetHotelDetails   — Full hotel page (images, facilities, rooms/rates).
    ///   Booking Flow:
    ///     3. CheckRate   — Confirm exact pricing before booking.
    ///     4. CreateBooking — Execute the reservation.
    ///   Booking Management:
    ///     5. GetUserBookings — List authenticated user's bookings from DB.
    ///     6. GetBookingDetail — Get live booking details from Hotelbeds API.
    ///     7. CancelBooking   — Cancel a booking and update DB.
    ///
    /// Every method returns a <see cref="ServiceResponse{T}"/> wrapper.
    /// </summary>
    public interface IHotelbedsService
    {
        // ============================
        // DISCOVERY
        // ============================

        /// <summary>
        /// Searches for destinations locally (autocomplete).
        /// </summary>
        Task<ServiceResponse<List<HotelDestination>>> SearchDestinationsAsync(
            string query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for available hotels matching the given criteria.
        /// Returns basic hotel cards with thumbnail images (merged from Availability + Content APIs).
        /// </summary>
        Task<ServiceResponse<HotelAvailabilityResponseDto>> SearchAvailabilityAsync(
            HotelAvailabilityRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets full hotel details: all images, description, facilities, contact info.
        /// If dates are provided, also returns live room availability with rates.
        /// </summary>
        /// <param name="hotelCode">The Hotelbeds hotel code.</param>
        /// <param name="query">Optional dates/occupancy for live availability.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ServiceResponse<HotelDetailResponseDto>> GetHotelDetailsAsync(
            int hotelCode,
            HotelDetailsQueryDto? query,
            CancellationToken cancellationToken = default);

        // ============================
        // BOOKING FLOW
        // ============================

        /// <summary>
        /// Validates and confirms the exact price and cancellation policies for a selected rate.
        /// MUST be called before booking.
        /// </summary>
        Task<ServiceResponse<HotelCheckRateResponseDto>> CheckRateAsync(
            HotelCheckRateRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes checkout: validates the rate via CheckRate, creates a PendingPayment DB record
        /// with serialized guest data, and creates a Stripe PaymentIntent with the converted/marked-up price.
        /// Returns the Stripe ClientSecret + BookingId + validated price.
        /// </summary>
        Task<ServiceResponse<CheckoutResponseDto>> InitCheckoutAsync(
            HotelBookingRequestDto request,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Called by the Stripe webhook ONLY. Takes an existing PendingPayment booking,
        /// deserializes GuestDataJson, calls the Hotelbeds Booking API directly,
        /// and UPDATES the existing row to Confirmed (or SupplierFailed on error).
        /// 
        /// CRITICAL: This method does NOT create new DB records — it updates existing ones.
        /// This prevents the double-booking bug where CreateBookingAsync inserted a second row.
        /// </summary>
        /// <param name="bookingId">The existing PendingPayment booking ID from Stripe metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Success with booking reference, or failure with reason.</returns>
        Task<ServiceResponse<HotelBookingResponseDto>> FulfillBookingFromWebhookAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default);

        // ============================
        // BOOKING MANAGEMENT
        // ============================

        /// <summary>
        /// Returns all hotel bookings for the authenticated user from the database.
        /// </summary>
        /// <param name="userId">The authenticated user's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ServiceResponse<List<UserHotelBookingDto>>> GetUserBookingsAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets live booking details from the Hotelbeds API by booking reference.
        /// Validates ownership: only the user who made the booking can view it.
        /// </summary>
        /// <param name="reference">The Hotelbeds booking reference (e.g., "1-234567").</param>
        /// <param name="userId">The authenticated user's ID (for ownership check).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ServiceResponse<BookingDetailResponseDto>> GetBookingDetailAsync(
            string reference,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a booking via the Hotelbeds API and updates the database record to Refunded.
        /// Validates ownership: only the user who made the booking can cancel it.
        /// </summary>
        /// <param name="reference">The Hotelbeds booking reference to cancel.</param>
        /// <param name="userId">The authenticated user's ID (for ownership check).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ServiceResponse<BookingCancellationResponseDto>> CancelBookingAsync(
            string reference,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
