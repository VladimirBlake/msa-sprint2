using System;

namespace create_booking.DTO;

public class BookingCreatedEvent
{
    public string UserId { get; set;}
    public string HotelId { get; set;}
    public string PromoCode { get; set;}
    public double DiscountPercent { get; set;}
    public double Price { get; set;}
    public DateTime? CreatedAt { get; set;} // ISO-8601
}
