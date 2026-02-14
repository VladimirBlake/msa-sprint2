using System;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace booking_history_service.Services;

public class JsonSerializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return default;
        }
        // Use your preferred JSON deserialization logic here
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(data));
    }
}
