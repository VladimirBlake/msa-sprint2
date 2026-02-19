using System;
using Microsoft.EntityFrameworkCore;

namespace booking_history_service.Data;

public class AppDbContext : DbContext
{
    public DbSet<Models.BookingHistory> BookingHistory { get; set;}

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions options) : base(options) {}
}
