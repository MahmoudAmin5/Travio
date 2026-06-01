using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Duffel;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;

namespace Travio.Core.Services.Payment
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IGenericRepository<FlightBooking> _flightRepo;
        private readonly IGenericRepository<HotelBooking> _hotelRepo;
        private readonly IDuffelFlightBookingService _flightBookingService;
        private readonly IHotelbedsService _hotelbedsService;
        private readonly IPaymentGatewayService _paymentGateway;
        private readonly ILogger<StripeWebhookService> _logger;

        public StripeWebhookService(
            IGenericRepository<FlightBooking> flightRepo,
            IGenericRepository<HotelBooking> hotelRepo,
            IDuffelFlightBookingService flightBookingService,
            IHotelbedsService hotelbedsService,
            IPaymentGatewayService paymentGateway,
            ILogger<StripeWebhookService> logger)
        {
            _flightRepo = flightRepo;
            _hotelRepo = hotelRepo;
            _flightBookingService = flightBookingService;
            _hotelbedsService = hotelbedsService;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<bool> ProcessPaymentSuccessAsync(PaymentIntent paymentIntent)
        {
            var stripeIntentId = paymentIntent.Id;
            var bookingType = paymentIntent.Metadata.GetValueOrDefault("BookingType");

            if (bookingType == "Hotel")
            {
                return await ProcessHotelBookingAsync(paymentIntent);
            }
            
            // Default to Flight for backward compatibility
            return await ProcessFlightBookingAsync(stripeIntentId);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HOTEL BOOKING — COMPLETE REWRITE
        // FIX CRITICAL-1: No longer calls CreateBookingAsync (which created duplicate rows).
        //                 Instead calls FulfillBookingFromWebhookAsync which UPDATES existing row.
        // FIX CRITICAL-4: No more fake "Guest User" fallback — FulfillBookingFromWebhookAsync
        //                 fails and triggers refund if GuestDataJson is corrupt.
        // FIX MAJOR-5: Uses IPaymentGatewayService for refunds instead of new RefundService().
        // ═══════════════════════════════════════════════════════════════════

        private async Task<bool> ProcessHotelBookingAsync(PaymentIntent paymentIntent)
        {
            var stripeIntentId = paymentIntent.Id;
            var bookingIdStr = paymentIntent.Metadata.GetValueOrDefault("BookingId");
            if (!Guid.TryParse(bookingIdStr, out var bookingId))
            {
                _logger.LogError("Webhook failed: Invalid BookingId in metadata for Intent {IntentId}.", stripeIntentId);
                return false;
            }

            var booking = await _hotelRepo.GetByIdAsync(bookingId);
            if (booking == null)
            {
                _logger.LogError("Webhook failed: No hotel booking found for BookingId {BookingId}, Intent {IntentId}.", bookingId, stripeIntentId);
                return false;
            }

            // ── 1. Idempotency Check ─────────────────────────────────────────
            if (booking.BookingStatus != HotelBookingStatus.PendingPayment)
            {
                _logger.LogInformation("Webhook ignored: Hotel booking {BookingId} status is {Status} (not PendingPayment).",
                    bookingId, booking.BookingStatus);
                return true;
            }

            // ── 2. The Lock (Optimistic Concurrency via RowVersion) ──────────
            // Set status to ProcessingWebhook and attempt to save.
            // If another thread already changed the RowVersion, a DbUpdateConcurrencyException is thrown.
            booking.BookingStatus = HotelBookingStatus.ProcessingWebhook;
            booking.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _hotelRepo.UpdateAsync(booking);
                await _hotelRepo.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                // Another thread won the race — this is expected behavior, not an error.
                _logger.LogWarning("RACE CONDITION AVOIDED: Another thread is processing Hotel Intent {IntentId}.", stripeIntentId);
                return true; // Return 200 OK to Stripe so it doesn't retry
            }

            // ── 3. FIX CRITICAL-1 + CRITICAL-4: Call FulfillBookingFromWebhookAsync ──
            // This method:
            //   - Reads the existing booking from DB
            //   - Deserializes GuestDataJson (FAILS if corrupt — no fake names)
            //   - Calls Hotelbeds Booking API directly
            //   - UPDATES the existing row (no duplicate INSERT)
            var hotelResult = await _hotelbedsService.FulfillBookingFromWebhookAsync(bookingId);

            if (!hotelResult.Success)
            {
                _logger.LogCritical("Hotelbeds booking FAILED for Intent {IntentId}, BookingId {BookingId}. Reason: {Reason}",
                    stripeIntentId, bookingId, hotelResult.Message);

                // ── COMPENSATING TRANSACTION: Refund the payment ─────────────
                // FIX MAJOR-5: Use IPaymentGatewayService instead of new RefundService()
                var refundResult = await _paymentGateway.RefundPaymentAsync(
                    stripeIntentId, "requested_by_customer");

                // Update booking status with failure details
                // Re-read the booking since FulfillBookingFromWebhookAsync may have modified it
                booking = await _hotelRepo.GetByIdAsync(bookingId);
                if (booking is not null)
                {
                    booking.BookingStatus = refundResult.Success
                        ? HotelBookingStatus.RefundIssued
                        : HotelBookingStatus.SupplierFailed;
                    booking.FailureReason = hotelResult.Message;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _hotelRepo.UpdateAsync(booking);
                    await _hotelRepo.SaveChangesAsync();
                }

                if (!refundResult.Success)
                {
                    _logger.LogCritical(
                        "CRITICAL: Refund FAILED for Intent {IntentId}. Error: {Error}. MANUAL INTERVENTION REQUIRED.",
                        stripeIntentId, refundResult.ErrorMessage);
                }

                return false;
            }

            // ── 4. Success — FulfillBookingFromWebhookAsync already updated the row ──
            _logger.LogInformation("Hotel booking CONFIRMED. BookingId: {BookingId}, Reference: {Ref}.",
                bookingId, hotelResult.Data?.BookingReference);

            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FLIGHT BOOKING — Unchanged (uses existing pattern)
        // ═══════════════════════════════════════════════════════════════════

        private async Task<bool> ProcessFlightBookingAsync(string stripeIntentId)
        {
            var spec = new BookingByStripeIntentIdSpec(stripeIntentId);
            var booking = await _flightRepo.FirstOrDefaultAsync(spec);

            if (booking == null)
            {
                _logger.LogError($"Webhook failed: No flight booking found for Intent {stripeIntentId}");
                return false;
            }

            if (booking.BookingStatus != FlightBookingStatus.PendingPayment)
            {
                _logger.LogInformation($"Webhook ignored. Flight booking status is currently {booking.BookingStatus}.");
                return true;
            }

            booking.BookingStatus = FlightBookingStatus.ProcessingWebhook;

            try
            {
                await _flightRepo.UpdateAsync(booking);
            }
            catch (Exception) 
            {
                _logger.LogWarning($"RACE CONDITION AVOIDED: Another thread is processing Flight Intent {stripeIntentId}.");
                return true; 
            }

            var savedPassengers = string.IsNullOrEmpty(booking.PassengersJson)
                ? new List<PassengerDetailsDto>()
                : JsonSerializer.Deserialize<List<PassengerDetailsDto>>(booking.PassengersJson);

            var orderRequest = new FlightOrderRequestDto
            {
                OfferId = booking.OfferId,
                Passengers = savedPassengers
            };

            var duffelResult = await _flightBookingService.CreateOrderAsync(orderRequest);

            if (!duffelResult.Success)
            {
                _logger.LogCritical($"Duffel booking failed for Intent {stripeIntentId}. Reason: {duffelResult.Message}");

                try
                {
                    var refundResult = await _paymentGateway.RefundPaymentAsync(
                        stripeIntentId, "requested_by_customer");
                    booking.BookingStatus = refundResult.Success
                        ? FlightBookingStatus.RefundRequest
                        : FlightBookingStatus.Failed;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"Refund failed for Flight Intent {stripeIntentId}. Error: {ex.Message}");
                    booking.BookingStatus = FlightBookingStatus.Failed;
                }

                await _flightRepo.UpdateAsync(booking);
                return false;
            }

            booking.PNR = duffelResult.Data.PNR;
            booking.BookingStatus = FlightBookingStatus.Confirmed;
            await _flightRepo.UpdateAsync(booking);

            return true;
        }
    }
}
