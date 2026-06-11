using Ardalis.Specification;
using Travio.Core.Domain.Entities.Hotelbeds;

namespace Travio.Core.Domain.Specifications.Hotels
{
    /// <summary>
    /// Specification to retrieve all hotel bookings for a specific user, ordered by creation date (newest first).
    /// Pushes the WHERE + ORDER BY to SQL instead of loading the entire HotelBookings table into memory.
    /// </summary>
    public class HotelBookingsByUserIdSpec : Specification<HotelBooking>
    {
        public HotelBookingsByUserIdSpec(string userId)
        {
            Query.Where(b => b.UserId == userId)
                 .OrderByDescending(b => b.CreatedAt);
        }
    }
}
