using System;
using Confluent.Kafka;

namespace create_booking.Services.Kafka;

public class KafkaClientHandle
{
    IProducer<byte[], byte[]> kafkaProducer;

    public KafkaClientHandle(IConfiguration config)
    {
        var conf = new ProducerConfig();
        config.GetSection("Kafka").Bind(conf);
        this.kafkaProducer = new ProducerBuilder<byte[], byte[]>(conf).Build();
    }

    public Handle Handle { get => this.kafkaProducer.Handle; }

    public void Dispose()
    {
        // Block until all outstanding produce requests have completed (with or
        // without error).
        kafkaProducer.Flush();
        kafkaProducer.Dispose();
    }
}
