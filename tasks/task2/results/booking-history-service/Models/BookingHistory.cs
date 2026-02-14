using System;
using System.ComponentModel.DataAnnotations;

namespace booking_history_service.Models;

public class BookingHistory
{
    [Key]
    public string Id { get; set;}
    public string UserId { get; set;}
    public string HotelId { get; set;}
    public string PromoCode { get; set;}
    public double DiscountPercent { get; set;}
    public double Price { get; set;}
    public DateTime? CreatedAt { get; set;} // ISO-8601
}
