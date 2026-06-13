using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Contracts.Services.Email
{
    public interface IEmailService
    {
        // Notice we only pass IDs, NOT the whole entity. 
        // This is a strict rule for background jobs to prevent massive database payloads.
        Task SendHotelTicketAsync(Guid bookingId, string userId);
        Task SendFlightTicketAsync(Guid bookingId, string userId);
    }
}
