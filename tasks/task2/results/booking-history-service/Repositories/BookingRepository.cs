using System;

namespace booking_history_service.Repositories;

public class BookingRepository : AppRepository<Models.BookingHistory>, IBookingRepository
{
    public BookingRepository(Data.AppDbContext context) : base(context)
    {
    }
}
