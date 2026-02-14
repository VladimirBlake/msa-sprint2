using System;
using System.Text.Json;
using Confluent.Kafka;

namespace create_booking.Services;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null)
            return null;

        return System.Text.Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(data));
    }
}
