using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Duffel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Travio.Core.Domain.Specifications.Duffel
{
    public class BookingByStripeIntentIdSpec : SingleResultSpecification<FlightBooking>
    {
        public BookingByStripeIntentIdSpec(string stripeIntentId)
        {
            Query.Where(booking => booking.StripePaymentIntentId == stripeIntentId);
        }
    }
}
