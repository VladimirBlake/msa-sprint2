using System;
using booking_history_service.Data;
using booking_history_service.Repositories;
using booking_history_service.Services;
using Microsoft.EntityFrameworkCore;

namespace booking_history_service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingHistoryDb")));
        
        
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddHostedService<KafkaBookingCreationConsumer>();

        return services;
    }
}
