using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Duffel;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;

namespace Travio.Core.Services.Payment
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IGenericRepository<FlightBooking> _bookingRepo;
        private readonly IDuffelFlightBookingService _flightBookingService;
        private readonly ILogger<StripeWebhookService> _logger;

        public StripeWebhookService(
            IGenericRepository<FlightBooking> bookingRepo,
            IDuffelFlightBookingService flightBookingService,
            ILogger<StripeWebhookService> logger)
        {
            _bookingRepo = bookingRepo;
            _flightBookingService = flightBookingService;
            _logger = logger;
        }

        public async Task<bool> ProcessPaymentSuccessAsync(string stripeIntentId)
        {
            var spec = new BookingByStripeIntentIdSpec(stripeIntentId);
            var booking = await _bookingRepo.FirstOrDefaultAsync(spec);

            if (booking == null)
            {
                _logger.LogError($"Webhook failed: No booking found for Intent {stripeIntentId}");
                return false;
            }

            // 2. Concurrency Defense: Check if already processed
            if (booking.BookingStatus == FlightBookingStatus.Confirmed)
            {
                _logger.LogInformation("Duplicate webhook caught. Booking is already confirmed.");
                return true;
            }

            // 3. Unpack the Passengers we saved during Checkout!
            var savedPassengers = string.IsNullOrEmpty(booking.PassengersJson)
                ? new List<PassengerDetailsDto>()
                : JsonSerializer.Deserialize<List<PassengerDetailsDto>>(booking.PassengersJson);

            // 4. Ask Duffel to actually buy the ticket
            var orderRequest = new FlightOrderRequestDto
            {
                OfferId = booking.OfferId,
                Passengers = savedPassengers
            };

            var duffelResult = await _flightBookingService.CreateOrderAsync(orderRequest);

            if (!duffelResult.Success)
            {
                // CRITICAL: If Duffel fails (flight sold out), we update status to Failed.
                // You must later write a background job to issue a Stripe Refund for this Intent!
                _logger.LogCritical($"Duffel booking failed for Intent {stripeIntentId}. Reason: {duffelResult.Message}");
                booking.BookingStatus = FlightBookingStatus.Failed;
                await _bookingRepo.UpdateAsync(booking);
                return false;
            }

            // 5. Success! Save the real Airline PNR and mark as confirmed
            booking.PNR = duffelResult.Data.PNR;
            booking.BookingStatus = FlightBookingStatus.Confirmed;

            try
            {
                // This triggers the RowVersion Optimistic Concurrency check
                await _bookingRepo.UpdateAsync(booking);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Concurrency error saving PNR {booking.PNR}. {ex.Message}");
                return false;
            }
        }
    }
}

