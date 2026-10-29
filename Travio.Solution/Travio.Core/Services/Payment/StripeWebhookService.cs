using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.Email;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
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
        private readonly IBackgroundJobClient _backgroundJobClient;

        public StripeWebhookService(
            IGenericRepository<FlightBooking> flightRepo,
            IGenericRepository<HotelBooking> hotelRepo,
            IDuffelFlightBookingService flightBookingService,
            IHotelbedsService hotelbedsService,
            IPaymentGatewayService paymentGateway,
            ILogger<StripeWebhookService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _flightRepo = flightRepo;
            _hotelRepo = hotelRepo;
            _flightBookingService = flightBookingService;
            _hotelbedsService = hotelbedsService;
            _paymentGateway = paymentGateway;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<bool> ProcessPaymentSuccessAsync(PaymentIntent paymentIntent)
        {
            var stripeIntentId = paymentIntent.Id;
            var bookingType = paymentIntent.Metadata.GetValueOrDefault("BookingType");

            if (bookingType == "Hotel")
            {
                return await ProcessHotelBookingAsync(paymentIntent);
            }

            return await ProcessFlightBookingAsync(paymentIntent);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HOTEL BOOKING — Atomic Lock Acquisition, Stale-Lease Recovery & Idempotent Refund
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

            // Rule 6: Lightweight projection to verify existence and inspect status + UserId
            var bookingMeta = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(b => b.Id == bookingId)
                .Select(b => new { b.Id, b.UserId, b.BookingStatus, b.UpdatedAt })
                .FirstOrDefaultAsync();

            if (bookingMeta == null)
            {
                _logger.LogError("Webhook failed: No hotel booking found for BookingId {BookingId}, Intent {IntentId}.", bookingId, stripeIntentId);
                return false;
            }

            var now = DateTime.UtcNow;
            var staleLeaseThreshold = now.AddMinutes(-3);

            // 1. Idempotency & Stale-Lease Check
            var canClaim = bookingMeta.BookingStatus == HotelBookingStatus.PendingPayment ||
                          (bookingMeta.BookingStatus == HotelBookingStatus.ProcessingWebhook &&
                           bookingMeta.UpdatedAt.HasValue &&
                           bookingMeta.UpdatedAt.Value < staleLeaseThreshold);

            if (!canClaim)
            {
                _logger.LogInformation("Webhook ignored: Hotel booking {BookingId} status is {Status}.",
                    bookingId, bookingMeta.BookingStatus);
                return true;
            }

            // 2. Atomic Set-Based Lock Acquisition (Rule 3: ExecuteUpdateAsync)
            //    Executes a single atomic UPDATE statement with an exclusive row lock on PK_HotelBookings,
            //    automatically incrementing SQL Server RowVersion and eliminating SELECT-then-UPDATE races.
            var rowsClaimed = await _hotelRepo.GetQueryable()
                .Where(b => b.Id == bookingId &&
                           (b.BookingStatus == HotelBookingStatus.PendingPayment ||
                           (b.BookingStatus == HotelBookingStatus.ProcessingWebhook && b.UpdatedAt < staleLeaseThreshold)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.BookingStatus, HotelBookingStatus.ProcessingWebhook)
                    .SetProperty(b => b.UpdatedAt, now));

            if (rowsClaimed == 0)
            {
                _logger.LogWarning("RACE CONDITION AVOIDED: Another thread claimed Hotel Intent {IntentId} for Booking {BookingId}.",
                    stripeIntentId, bookingId);
                return true;
            }

            // 3. Supplier Fulfillment via Hotelbeds API
            var hotelResult = await _hotelbedsService.FulfillBookingFromWebhookAsync(bookingId);

            if (!hotelResult.Success)
            {
                _logger.LogCritical("Hotelbeds booking FAILED for Intent {IntentId}, BookingId {BookingId}. Reason: {Reason}",
                    stripeIntentId, bookingId, hotelResult.Message);

                // Compensating Transaction: Issue idempotent refund tied to bookingId
                var refundResult = await _paymentGateway.RefundPaymentAsync(
                    stripeIntentId, "requested_by_customer", bookingId);

                var finalFailureStatus = refundResult.Success
                    ? HotelBookingStatus.RefundIssued
                    : HotelBookingStatus.SupplierFailed;

                // Rule 3: Set-based update of failure status without loading or overwriting GuestDataJson
                await _hotelRepo.GetQueryable()
                    .Where(b => b.Id == bookingId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BookingStatus, finalFailureStatus)
                        .SetProperty(b => b.FailureReason, hotelResult.Message)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow));

                if (!refundResult.Success)
                {
                    _logger.LogCritical(
                        "CRITICAL: Refund FAILED for Intent {IntentId}. Error: {Error}. MANUAL INTERVENTION REQUIRED.",
                        stripeIntentId, refundResult.ErrorMessage);
                }

                return true;
            }

            // 4. Success — Enqueue confirmation email job via Hangfire
            _backgroundJobClient.Enqueue<IEmailService>(emailService =>
                emailService.SendHotelTicketAsync(bookingId, bookingMeta.UserId));

            _logger.LogInformation("Hotel booking CONFIRMED. BookingId: {BookingId}, Reference: {Ref}.",
                bookingId, hotelResult.Data?.BookingReference);

            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FLIGHT BOOKING — Atomic Lock Acquisition, Stale-Lease Recovery & Idempotent Refund
        // ═══════════════════════════════════════════════════════════════════

        private async Task<bool> ProcessFlightBookingAsync(PaymentIntent paymentIntent)
        {
            var stripeIntentId = paymentIntent.Id;
            var bookingIdStr = paymentIntent.Metadata.GetValueOrDefault("BookingId");
            Guid.TryParse(bookingIdStr, out var metadataBookingId);

            // Rule 6: Direct projection on GetQueryable() using PK or Unique Filtered Index IX_FlightBookings_StripePaymentIntentId
            var booking = await _flightRepo.GetQueryable()
                .AsNoTracking()
                .Where(b => (metadataBookingId != Guid.Empty && b.Id == metadataBookingId) ||
                            b.StripePaymentIntentId == stripeIntentId)
                .Select(b => new
                {
                    b.Id,
                    b.UserId,
                    b.OfferId,
                    b.TotalPrice,
                    b.Currency,
                    b.PassengersJson,
                    b.BookingStatus,
                    b.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                _logger.LogError("Webhook failed: No flight booking found for Intent {IntentId}", stripeIntentId);
                return false;
            }

            var now = DateTime.UtcNow;
            var staleLeaseThreshold = now.AddMinutes(-3);

            // 1. Idempotency & Stale-Lease Check
            var canClaim = booking.BookingStatus == FlightBookingStatus.PendingPayment ||
                          (booking.BookingStatus == FlightBookingStatus.ProcessingWebhook &&
                           booking.UpdatedAt.HasValue &&
                           booking.UpdatedAt.Value < staleLeaseThreshold);

            if (!canClaim)
            {
                _logger.LogInformation("Webhook ignored: Flight booking {BookingId} is in status {Status}.",
                    booking.Id, booking.BookingStatus);
                return true;
            }

            // 2. Atomic Set-Based Lock Acquisition (Rule 3: ExecuteUpdateAsync)
            var rowsClaimed = await _flightRepo.GetQueryable()
                .Where(b => b.Id == booking.Id &&
                           (b.BookingStatus == FlightBookingStatus.PendingPayment ||
                           (b.BookingStatus == FlightBookingStatus.ProcessingWebhook && b.UpdatedAt < staleLeaseThreshold)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.BookingStatus, FlightBookingStatus.ProcessingWebhook)
                    .SetProperty(b => b.UpdatedAt, now));

            if (rowsClaimed == 0)
            {
                _logger.LogWarning("RACE CONDITION AVOIDED: Another thread just claimed Flight Intent {IntentId}.", stripeIntentId);
                return true;
            }

            var savedPassengers = string.IsNullOrEmpty(booking.PassengersJson)
                ? new List<PassengerDetailsDto>()
                : JsonSerializer.Deserialize<List<PassengerDetailsDto>>(booking.PassengersJson) ?? new List<PassengerDetailsDto>();

            var orderRequest = new FlightOrderRequestDto
            {
                OfferId = booking.OfferId,
                TotalAmount = booking.TotalPrice,
                Currency = booking.Currency,
                Passengers = savedPassengers
            };

            var duffelResult = await _flightBookingService.CreateOrderAsync(orderRequest);

            if (!duffelResult.Success)
            {
                _logger.LogCritical("Duffel booking failed for Intent {IntentId}. Reason: {Reason}", stripeIntentId, duffelResult.Message);

                bool isRefunded = false;
                try
                {
                    var refundResult = await _paymentGateway.RefundPaymentAsync(stripeIntentId, "requested_by_customer", booking.Id);
                    isRefunded = refundResult.Success;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Refund failed for Flight Intent {IntentId}.", stripeIntentId);
                }

                var failureStatus = isRefunded
                    ? FlightBookingStatus.RefundRequest
                    : FlightBookingStatus.Failed;

                // Rule 3: Set-based update of failure state
                await _flightRepo.GetQueryable()
                    .Where(b => b.Id == booking.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BookingStatus, failureStatus)
                        .SetProperty(b => b.FailureReason, duffelResult.Message)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow));

                return true;
            }

            var confirmedPnr = duffelResult.Data.PNR;

            // Rule 3: Set-based update of confirmation state (updates only PNR, BookingStatus, UpdatedAt)
            var updatedRows = await _flightRepo.GetQueryable()
                .Where(b => b.Id == booking.Id && b.BookingStatus != FlightBookingStatus.Confirmed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.PNR, confirmedPnr)
                    .SetProperty(b => b.BookingStatus, FlightBookingStatus.Confirmed)
                    .SetProperty(b => b.UpdatedAt, DateTime.UtcNow));

            if (updatedRows > 0)
            {
                _backgroundJobClient.Enqueue<IEmailService>(emailService =>
                    emailService.SendFlightTicketAsync(booking.Id, booking.UserId));
            }

            return true;
        }
    }
}
