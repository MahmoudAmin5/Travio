using Ardalis.Specification;
using Travio.Core.Domain.Entities.Hotelbeds;

namespace Travio.Core.Domain.Specifications.Hotels
{
    /// <summary>
    /// Specification to find a hotel booking by its Hotelbeds reference string.
    /// Used by GetBookingDetail and CancelBooking for ownership verification.
    /// </summary>
    public class HotelBookingByReferenceSpec : SingleResultSpecification<HotelBooking>
    {
        public HotelBookingByReferenceSpec(string hotelbedsReference)
        {
            Query.Where(b => b.HotelbedsReference == hotelbedsReference);
        }
    }
}
