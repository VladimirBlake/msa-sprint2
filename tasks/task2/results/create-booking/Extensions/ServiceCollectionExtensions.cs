using Confluent.Kafka;
using create_booking.Data;
using create_booking.DTO;
using create_booking.Repositories;
using create_booking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace create_booking.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingDb")));

        services.AddHttpClient<IHotelioMonolithClient, HotelioMonolithClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(configuration["Hotelio:MonolithBaseUrl"] ?? "http://localhost:8080");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        services.AddSingleton<IProducer<string, BookingCreatedEvent>>(sp =>
        {
            var kafkaConfig = new ProducerConfig();
            configuration.GetSection("Kafka").Bind(kafkaConfig);
            return new ProducerBuilder<string, BookingCreatedEvent>(kafkaConfig)
                .SetValueSerializer(new JsonSerializer<BookingCreatedEvent>())
                .Build();
        });

        services.AddScoped<IMonolithConfiguration, MonolithConfiguration>();
        services.AddScoped<IBookingValidator, BookingValidator>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}


