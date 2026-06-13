using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Contracts.Services.Email;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Infrastructure.Contract;

namespace Travio.Core.Services.Shared.Email
{
    public class EmailService : IEmailService
    {
        private readonly IGenericRepository<HotelBooking> _hotelRepo;
        private readonly IGenericRepository<FlightBooking> _flightRepo;
        private readonly UserManager<ApplicationUser> _userManager; // Used to fetch the email
        private readonly ILogger<EmailService> _logger;
         private readonly IEmailSender _emailProvider; // Inject your actual email sender here (SendGrid, MailKit, etc.)

        public EmailService(
            IGenericRepository<HotelBooking> hotelRepo,
            IGenericRepository<FlightBooking> flightRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<EmailService> logger,
            IEmailSender emailSender)
        {
            _hotelRepo = hotelRepo;
            _flightRepo = flightRepo;
            _userManager = userManager;
            _logger = logger;
            _emailProvider = emailSender;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HOTEL TICKET GENERATOR
        // ═══════════════════════════════════════════════════════════════════
        public async Task SendHotelTicketAsync(Guid bookingId, string userId)
        {
            _logger.LogInformation("Generating Hotel Ticket for Booking {BookingId}", bookingId);

            try
            {
                var booking = await _hotelRepo.GetByIdAsync(bookingId);
                if (booking == null) return;

                // 1. Get the exact customer email using the UserId from the booking record
                var user = await _userManager.FindByIdAsync(userId);
                var customerEmail = user?.Email;

                if (string.IsNullOrEmpty(customerEmail))
                {
                    _logger.LogError("Cannot send email. No email found for UserId {UserId}", userId);
                    return;
                }

                // 2. Generate the Hotel HTML Template using Travio Design System
                var htmlBody = $"""
                <!DOCTYPE html>
                <html>
                <body style="background-color: #F4F5F7; margin: 0; padding: 40px 20px; font-family: Arial, sans-serif;">
                    <table border="0" cellpadding="0" cellspacing="0" width="100%" style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                        <!-- Logo Section -->
                                    <tr>
                                        <td align="center" style="padding: 48px 24px 32px 24px;">
                                            <img src="https://res.cloudinary.com/dn8tsma3t/image/upload/v1773413577/travlo22_b5j7mo.png" alt="Travlo" width="150" style="display: block; width: 130px; max-width: 100%; height: auto;">
                                        </td>
                                    </tr>
                        </tr>
                        <tr><td style="height: 4px; background-color: #F2A900;"></td></tr>
                        <tr>
                            <td style="padding: 40px;">
                                <h1 style="margin: 0 0 20px 0; color: #006666; font-size: 24px; text-align: center;">Hotel Confirmation</h1>
                                <p style="color: #555555; line-height: 1.6;">Hello {user?.FirstName ?? "Traveler"},</p>
                                <p style="color: #555555; line-height: 1.6;">Your hotel is officially booked and confirmed. Here are your reservation details:</p>
                                
                                <div style="background-color: #f9fafc; border-left: 4px solid #006666; padding: 15px; margin: 25px 0;">
                                    <h2 style="margin: 0 0 10px 0; font-size: 18px; color: #333333;">{booking.HotelName}</h2>
                                    <p style="margin: 5px 0; color: #555555;"><strong>Check-in:</strong> {booking.CheckIn:MMM dd, yyyy}</p>
                                    <p style="margin: 5px 0; color: #555555;"><strong>Check-out:</strong> {booking.CheckOut:MMM dd, yyyy}</p>
                                    <p style="margin: 5px 0; color: #555555;"><strong>Rooms:</strong> {booking.RoomCount}</p>
                                </div>

                                <div style="text-align: center; margin-top: 30px;">
                                    <p style="font-size: 14px; color: #888888; text-transform: uppercase; margin-bottom: 5px;">Hotelbeds Booking Reference</p>
                                    <h2 style="margin: 0; color: #006666; font-size: 28px; letter-spacing: 2px;">{booking.HotelbedsReference}</h2>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td align="center" style="padding: 20px; background-color: #f9fafc; color: #888888; font-size: 12px;">
                                © {DateTime.UtcNow.Year} Travio. Safe travels!
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;

                // 3. Send the email directly to the customer
                 await _emailProvider.SendEmailAsync(customerEmail, $"Hotel Confirmation: {booking.HotelName}", htmlBody);

                _logger.LogInformation("Hotel Ticket sent successfully to {Email}", customerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send hotel ticket for {BookingId}", bookingId);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // FLIGHT E-TICKET GENERATOR
        // ═══════════════════════════════════════════════════════════════════
        public async Task SendFlightTicketAsync(Guid bookingId, string userId)
        {
            _logger.LogInformation("Generating Flight Ticket for Booking {BookingId}", bookingId);

            try
            {
                var booking = await _flightRepo.GetByIdAsync(bookingId);
                if (booking == null) return;

                // 1. Get the exact customer email using the UserId from the booking record
                var user = await _userManager.FindByIdAsync(userId);
                var customerEmail = user?.Email;

                if (string.IsNullOrEmpty(customerEmail))
                {
                    _logger.LogError("Cannot send email. No email found for UserId {UserId}", userId);
                    return;
                }

                // 2. Generate the Flight HTML Template using Travio Design System
                var htmlBody = $"""
                <!DOCTYPE html>
                <html>
                <body style="background-color: #F4F5F7; margin: 0; padding: 40px 20px; font-family: Arial, sans-serif;">
                    <table border="0" cellpadding="0" cellspacing="0" width="100%" style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);">
                        <!-- Logo Section -->
                                    <tr>
                                        <td align="center" style="padding: 48px 24px 32px 24px;">
                                            <img src="https://res.cloudinary.com/dn8tsma3t/image/upload/v1773413577/travlo22_b5j7mo.png" alt="Travlo" width="150" style="display: block; width: 130px; max-width: 100%; height: auto;">
                                        </td>
                                    </tr>
                        <tr><td style="height: 4px; background-color: #006666;"></td></tr>
                        <tr>
                            <td style="padding: 40px;">
                                <h1 style="margin: 0 0 20px 0; color: #006666; font-size: 24px; text-align: center;">E-Ticket Confirmed ✈️</h1>
                                <p style="color: #555555; line-height: 1.6;">Hello {user?.FirstName ?? "Traveler"},</p>
                                <p style="color: #555555; line-height: 1.6;">Your flight has been ticketed. Please keep this email handy for your records and check-in process.</p>
                                
                                <div style="background-color: #FFF9E6; border-left: 4px solid #F2A900; padding: 15px; margin: 25px 0;">
                                    <p style="margin: 5px 0; color: #555555;"><strong>Status:</strong> <span style="color: #006666; font-weight: bold;">Confirmed</span></p>
                                    <p style="margin: 5px 0; color: #555555;"><strong>Amount Paid:</strong> {booking.Currency} {booking.TotalPrice}</p>
                                </div>

                                <div style="text-align: center; margin-top: 30px; padding: 20px; border: 1px dashed #cccccc; border-radius: 8px;">
                                    <p style="font-size: 14px; color: #888888; text-transform: uppercase; margin-bottom: 5px;">Passenger Name Record (PNR)</p>
                                    <h2 style="margin: 0; color: #F2A900; font-size: 32px; letter-spacing: 4px;">{booking.PNR}</h2>
                                    <p style="margin: 10px 0 0 0; font-size: 12px; color: #888888;">Use this code on the airline's website to manage your flight.</p>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td align="center" style="padding: 20px; background-color: #f9fafc; color: #888888; font-size: 12px;">
                                © {DateTime.UtcNow.Year} Travio. Have a great flight!
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;

                // 3. Send the email directly to the customer
                 await _emailProvider.SendEmailAsync(customerEmail, $"Flight E-Ticket (PNR: {booking.PNR})", htmlBody);

                _logger.LogInformation("Flight E-Ticket sent successfully to {Email}", customerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send flight ticket for {BookingId}", bookingId);
                throw;
            }
        }
    }
}

