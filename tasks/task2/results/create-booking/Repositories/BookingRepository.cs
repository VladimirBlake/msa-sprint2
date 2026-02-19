using System;

namespace create_booking.Repositories;

public class BookingRepository : AppRepository<Models.Booking>, IBookingRepository
{
    public BookingRepository(Data.AppDbContext context) : base(context)
    {
    }
}
