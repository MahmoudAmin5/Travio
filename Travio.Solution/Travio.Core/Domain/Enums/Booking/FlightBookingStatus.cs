using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Enums.Booking
{
    public enum FlightBookingStatus
    {
        PendingPayment,
        Confirmed,
        Failed,
        Cancelled
    }
}
