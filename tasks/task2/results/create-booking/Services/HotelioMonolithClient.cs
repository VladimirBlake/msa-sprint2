using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace create_booking.Services;

/// <summary>
/// Client for communicating with Hotelio Monolith service.
/// Provides abstraction for REST API calls to validate users, hotels, promo codes, and reviews.
/// </summary>
public interface IHotelioMonolithClient
{
    Task<UserResponse> GetUserAsync(string userId);
    Task<HotelResponse> GetHotelAsync(string hotelId);
    Task<PromoCodeResponse> ValidatePromoCodeAsync(string code, string userId);
    Task<bool> IsHotelTrustedAsync(string hotelId);
}

public class HotelioMonolithClient : IHotelioMonolithClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HotelioMonolithClient> _logger;
    private readonly string _baseUrl;

    public HotelioMonolithClient(
        HttpClient httpClient,
        ILogger<HotelioMonolithClient> logger,
        IMonolithConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = config.MonolithBaseUrl;
    }
    public async Task<UserResponse> GetUserAsync(string userId)
    {
        try
        {
            var url = $"{_baseUrl}/api/users/{userId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var json = await response.Content.ReadAsStringAsync();
            var content = JsonSerializer.Deserialize<UserResponse>(json, options);
            _logger.LogInformation("Successfully retrieved user: {UserId}", userId);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            throw new HotelioServiceException($"Failed to retrieve user {userId}", ex);
        }
    }
    public async Task<HotelResponse> GetHotelAsync(string hotelId)
    {
        try
        {
            var url = $"{_baseUrl}/api/hotels/{hotelId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


            var json = await response.Content.ReadAsStringAsync();
            var content = JsonSerializer.Deserialize<HotelResponse>(json, options);
            _logger.LogInformation("Successfully retrieved hotel: {HotelId}", hotelId);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get hotel {HotelId}", hotelId);
            throw new HotelioServiceException($"Failed to retrieve hotel {hotelId}", ex);
        }
    }

    public async Task<PromoCodeResponse> ValidatePromoCodeAsync(string code, string userId)
    {
        try
        {
            var url = $"{_baseUrl}/api/promos/validate?code={Uri.EscapeDataString(code)}&userId={Uri.EscapeDataString(userId)}";
            var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = await response.Content.ReadAsStringAsync();
            var content = JsonSerializer.Deserialize<PromoCodeResponse>(json, options);
            _logger.LogInformation("Successfully validated promo code: {Code} for user: {UserId}", code, userId);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to validate promo code {Code}", code);
            throw new HotelioServiceException($"Failed to validate promo code {code}", ex);
        }
    }

    public async Task<bool> IsHotelTrustedAsync(string hotelId)
    {
        try
        {
            var url = $"{_baseUrl}/api/reviews/hotel/{hotelId}/trusted";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


            var json = await response.Content.ReadAsStringAsync();
            var content = JsonSerializer.Deserialize<bool>(json, options);
            _logger.LogInformation("Successfully checked trust status for hotel: {HotelId}", hotelId);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to check trust status for hotel {HotelId}", hotelId);
            throw new HotelioServiceException($"Failed to check trust status for hotel {hotelId}", ex);
        }
    }
}

// Response DTOs
public class UserResponse
{
    public string Id { get; set; }
    public string Status { get; set; }
    public bool Blacklisted { get; set; }
    public bool Active { get; set; }
}

public class HotelResponse
{
    public string Id { get; set; }
    public bool Operational { get; set; }
    public bool FullyBooked { get; set; }
    public string City { get; set; }
    public double Rating { get; set; }
}

public class PromoCodeResponse
{
    public string Code { get; set; }
    public double Discount { get; set; }
    public bool Expired { get; set; }
    public bool VipOnly { get; set; }
}

public class ReviewTrustResponse
{
    public bool Trusted { get; set; }
}

// Custom exception for Hotelio service errors
public class HotelioServiceException : Exception
{
    public HotelioServiceException(string message) : base(message) { }
    public HotelioServiceException(string message, Exception innerException)
        : base(message, innerException) { }
}
