using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Duffel;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;
using Travio.Core.DTOs.HotelbedsDTOs.Requests;

namespace Travio.Core.Services.Payment
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IGenericRepository<FlightBooking> _flightRepo;
        private readonly IGenericRepository<HotelBooking> _hotelRepo;
        private readonly IDuffelFlightBookingService _flightBookingService;
        private readonly IHotelbedsService _hotelbedsService;
        private readonly ILogger<StripeWebhookService> _logger;

        public StripeWebhookService(
            IGenericRepository<FlightBooking> flightRepo,
            IGenericRepository<HotelBooking> hotelRepo,
            IDuffelFlightBookingService flightBookingService,
            IHotelbedsService hotelbedsService,
            ILogger<StripeWebhookService> logger)
        {
            _flightRepo = flightRepo;
            _hotelRepo = hotelRepo;
            _flightBookingService = flightBookingService;
            _hotelbedsService = hotelbedsService;
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

            // ── 3. Deserialize guest data from the DB record ─────────────────
            HotelBookingRequestDto? guestRequest = null;
            if (!string.IsNullOrWhiteSpace(booking.GuestDataJson))
            {
                try
                {
                    guestRequest = JsonSerializer.Deserialize<HotelBookingRequestDto>(booking.GuestDataJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize GuestDataJson for BookingId {BookingId}.", bookingId);
                }
            }

            // Fallback: If deserialization fails, construct a minimal request with the stored rate key
            guestRequest ??= new HotelBookingRequestDto
            {
                RateKey = booking.RateKey,
                HolderFirstName = "Guest",
                HolderLastName = "User",
                Rooms = new List<BookingRoomDto>
                {
                    new BookingRoomDto
                    {
                        RateKey = booking.RateKey,
                        Paxes = new List<BookingPaxDto>
                        {
                            new BookingPaxDto { RoomId = 1, Type = "AD", Name = "Guest", Surname = "User" }
                        }
                    }
                }
            };

            // ── 4. Call Hotelbeds Booking API ─────────────────────────────────
            var hotelResult = await _hotelbedsService.CreateBookingAsync(guestRequest, booking.UserId);

            if (!hotelResult.Success)
            {
                _logger.LogCritical("Hotelbeds booking FAILED for Intent {IntentId}, BookingId {BookingId}. Reason: {Reason}",
                    stripeIntentId, bookingId, hotelResult.Message);

                // ── COMPENSATING TRANSACTION: Refund the payment ─────────────
                try
                {
                    var refundOptions = new RefundCreateOptions
                    {
                        PaymentIntent = stripeIntentId,
                        Reason = RefundReasons.RequestedByCustomer
                    };
                    await new RefundService().CreateAsync(refundOptions);
                    _logger.LogInformation("Refund issued successfully for Intent {IntentId}.", stripeIntentId);
                }
                catch (StripeException ex)
                {
                    _logger.LogCritical(ex, "CRITICAL: Refund FAILED for Intent {IntentId}. Manual intervention required.", stripeIntentId);
                }

                booking.BookingStatus = HotelBookingStatus.PaymentFailed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _hotelRepo.UpdateAsync(booking);
                await _hotelRepo.SaveChangesAsync();
                return false;
            }

            // ── 5. Success — Update booking with Hotelbeds reference ─────────
            booking.HotelbedsReference = hotelResult.Data?.BookingReference;
            booking.BookingStatus = HotelBookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _hotelRepo.UpdateAsync(booking);
            await _hotelRepo.SaveChangesAsync();

            _logger.LogInformation("Hotel booking CONFIRMED. BookingId: {BookingId}, Reference: {Ref}.",
                bookingId, booking.HotelbedsReference);

            return true;
        }

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
                    var refundOptions = new RefundCreateOptions { PaymentIntent = stripeIntentId, Reason = RefundReasons.RequestedByCustomer };
                    await new RefundService().CreateAsync(refundOptions);
                    booking.BookingStatus = FlightBookingStatus.RefundRequest;
                }
                catch (StripeException stripeEx)
                {
                    _logger.LogCritical($"Refund failed for Flight Intent {stripeIntentId}. Error: {stripeEx.Message}");
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

