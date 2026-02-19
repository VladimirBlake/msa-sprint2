using System;
using Microsoft.EntityFrameworkCore;

namespace create_booking.Data;

public class AppDbContext : DbContext
{
    public DbSet<Models.Booking> Bookings { get; set;}

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }
}
