using System;
using System.ComponentModel.DataAnnotations;

namespace create_booking.Services;

/// <summary>
/// Configuration for Hotelio Monolith service connectivity.
/// </summary>
public interface IMonolithConfiguration
{
    string MonolithBaseUrl { get; }
}

public class MonolithConfiguration : IMonolithConfiguration
{
    [Required(ErrorMessage = "MonolithBaseUrl must be configured")]
    public string MonolithBaseUrl { get; set; }

    public MonolithConfiguration(IConfiguration configuration)
    {
        MonolithBaseUrl = configuration["Hotelio:MonolithBaseUrl"]
            ?? throw new InvalidOperationException("Hotelio:MonolithBaseUrl is not configured");
    }
}
