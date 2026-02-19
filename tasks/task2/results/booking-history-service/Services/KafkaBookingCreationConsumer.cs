using System;
using booking_history_service.DTO;
using booking_history_service.Models;
using booking_history_service.Repositories;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace booking_history_service.Services;

public class KafkaBookingCreationConsumer : BackgroundService
{
    private readonly string topic;
    private readonly string bootstrapServers;
    private readonly ILogger<KafkaBookingCreationConsumer> _logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IConsumer<string, BookingCreatedEvent> kafkaConsumer;

    public KafkaBookingCreationConsumer(ILogger<KafkaBookingCreationConsumer> logger, IConfiguration config, IServiceScopeFactory serviceScopeFactory)
    {
         var consumerConfig = new ConsumerConfig();
        config.GetSection("Kafka:ConsumerSettings").Bind(consumerConfig);
        bootstrapServers = consumerConfig.BootstrapServers;
        this.topic = config.GetValue<string>("Kafka:Topic");
        this.kafkaConsumer = new ConsumerBuilder<string, BookingCreatedEvent>(consumerConfig)
            .SetValueDeserializer(new JsonSerializer<BookingCreatedEvent>())
            .Build();
        this.serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _logger.LogInformation($"Consumer service initialized successfully! Kafka Bootstrap Server: {consumerConfig.BootstrapServers}");
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        await EnsureTopicExists(topic, bootstrapServers);
        kafkaConsumer.Subscribe(topic);
        _logger.LogInformation($"Successfully subscribed to topic {topic}");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = kafkaConsumer.Consume(cancellationToken);
                
                if (cr.Message != null)
                {
                    _logger.LogInformation($"Received message from topic: {topic}!");
                    using (var scope = serviceScopeFactory.CreateScope())
                    {
                        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                        var booking = new BookingHistory
                        {
                            Id = cr.Message.Key,
                            UserId = cr.Message.Value.UserId,
                            HotelId = cr.Message.Value.HotelId,
                            PromoCode = cr.Message.Value.PromoCode,
                            DiscountPercent = cr.Message.Value.DiscountPercent,
                            Price = cr.Message.Value.Price,
                            CreatedAt = cr.Message.Value.CreatedAt
                        };

                        await bookingRepository.AddAsync(booking);
                        await bookingRepository.SaveChangesAsync();
                    }
                    kafkaConsumer.Commit(cr);
                }

            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                // Consumer errors should generally be ignored (or logged) unless fatal.
                Console.WriteLine($"Consume error: {e.Error.Reason}");
                _logger.LogError("Error occurs while trying to consume message :(");

                if (e.Error.IsFatal)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e}");
                break;
            }
        }
    }

    public override void Dispose()
    {
        this.kafkaConsumer.Close(); // Commit offsets and leave the group cleanly.
        this.kafkaConsumer.Dispose();

        base.Dispose();
    }

    public async Task EnsureTopicExists(string topicName, string bootstrapServers)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

        var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));

        if (metadata.Topics.Any(t => t.Topic == topicName))
            return;

        await admin.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = topicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        });
    }
}
