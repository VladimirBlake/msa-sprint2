using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using create_booking.Interfaces;

namespace create_booking.Models;

public class Booking : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set;}
    public string UserId { get; set;}
    public string HotelId { get; set;}
    public string? PromoCode { get; set;}
    public double DiscountPercent { get; set;}
    public double Price { get; set;}
    public DateTime? CreatedAt { get; set;} // ISO-8601
}
