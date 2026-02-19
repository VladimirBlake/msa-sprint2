using System;
using System.Threading.Tasks;
using Booking;
using Microsoft.Extensions.Logging;

namespace create_booking.Services;

/// <summary>
/// Validator for booking requests.
/// Performs all necessary validations: user status, hotel availability, promo code validity, and hotel trust.
/// </summary>
public interface IBookingValidator
{
    Task<BookingValidationResult> ValidateAsync(BookingRequest request);
}

public class BookingValidator : IBookingValidator
{
    private readonly IHotelioMonolithClient _client;
    private readonly ILogger<BookingValidator> _logger;
    public BookingValidator(
        IHotelioMonolithClient client,
        ILogger<BookingValidator> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<BookingValidationResult> ValidateAsync(BookingRequest request)
    {
        _logger.LogInformation("Starting validation for booking request: UserId={UserId}, HotelId={HotelId}, PromoCode={PromoCode}",
            request.UserId, request.HotelId, request.PromoCode);

        try
        {
            // Validate user
            var user = await _client.GetUserAsync(request.UserId);
            if (!ValidateUser(user, out var userError))
            {
                _logger.LogWarning("User validation failed: {Error}", userError);
                return BookingValidationResult.Failure(userError);
            }

            // Validate hotel
            var hotel = await _client.GetHotelAsync(request.HotelId);
            if (!ValidateHotel(hotel, out var hotelError))
            {
                _logger.LogWarning("Hotel validation failed: {Error}", hotelError);
                return BookingValidationResult.Failure(hotelError);
            }

            // Validate hotel trust
            var trustStatus = await _client.IsHotelTrustedAsync(request.HotelId);
            if (!ValidateHotelTrust(trustStatus, out var trustError))
            {
                _logger.LogWarning("Hotel trust validation failed: {Error}", trustError);
                return BookingValidationResult.Failure(trustError);
            }

            // Validate and calculate promo code discount
            double discountPercent = 0;
            double hotelPrice = GetHotelBasePrice(user);
            double discount = 0;
            if (!string.IsNullOrEmpty(request.PromoCode))
            {
                var promoCode = await _client.ValidatePromoCodeAsync(request.PromoCode, request.UserId);
                if (!ValidatePromoCode(promoCode, user, out var promoError, out discountPercent))
                {
                    _logger.LogWarning("Promo code validation failed: {Error}", promoError);
                    return BookingValidationResult.Failure(promoError);
                }

                discount = GetPromoDiscount(promoCode);
            }

            _logger.LogInformation("Booking validation succeeded");
            return BookingValidationResult.Success(hotelPrice, discount);
        }
        catch (HotelioServiceException ex)
        {
            _logger.LogError(ex, "External service error during booking validation");
            return BookingValidationResult.Failure($"Service unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during booking validation");
            return BookingValidationResult.Failure("An unexpected error occurred during validation");
        }
    }

    private bool ValidateUser(UserResponse user, out string error)
    {
        error = null;

        if (user.Blacklisted)
        {
            error = "User is blacklisted";
            return false;
        }

        if (!user.Active)
        {
            error = $"User is not active. Status: {user.Active}";
            return false;
        }

        return true;
    }

    private bool ValidateHotel(HotelResponse hotel, out string error)
    {
        error = null;

        if (!hotel.Operational)
        {
            error = $"Hotel {hotel.Id} is not operational!";
            return false;
        }

        if (hotel.FullyBooked)
        {
            error = $"Hotel {hotel.Id} is fully booked!";
            return false;
        }

        return true;
    }

    private bool ValidateHotelTrust(bool trustStatus, out string error)
    {
        error = null;

        if (!trustStatus)
        {
            error = "Hotel does not meet trust requirements";
            return false;
        }

        return true;
    }

    private bool ValidatePromoCode(
        PromoCodeResponse promoCode,
        UserResponse user,
        out string error,
        out double discountPercent)
    {
        error = null;
        discountPercent = 0;

        if (promoCode.Expired)
        {
            error = "Promo code is not active";
            return false;
        }

        if (promoCode.VipOnly && user.Status != "VIP")
        {
            error = "Promo code is available only for VIP users";
            return false;
        }

        discountPercent = promoCode.Discount;
        return true;
    }

    private double GetHotelBasePrice(UserResponse user)
        => user.Status.ToUpper() == "VIP" ? 80 : 100;

    private double GetPromoDiscount(PromoCodeResponse promoCode)
        => promoCode.Discount;
}

/// <summary>
/// Result of booking validation operation.
/// </summary>
public class BookingValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; }
    public double BasePrice { get; set; }
    public double Discount { get; set; }

    public static BookingValidationResult Success(double basePrice, double discount)
    {
        return new BookingValidationResult
        {
            IsValid = true,
            BasePrice = basePrice,
            Discount = discount
        };
    }

    public static BookingValidationResult Failure(string error)
    {
        return new BookingValidationResult
        {
            IsValid = false,
            Error = error
        };
    }
}
