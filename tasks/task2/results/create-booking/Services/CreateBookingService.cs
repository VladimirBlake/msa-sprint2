using System;
using System.Threading.Tasks;
using Booking;
using Confluent.Kafka;
using create_booking.DTO;
using create_booking.Models;
using create_booking.Repositories;
using create_booking.Services.Kafka;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace create_booking.Services;


public class CreateBookingService : BookingService.BookingServiceBase {
    private readonly IBookingValidator _validator;
    private readonly IBookingRepository _repository;
    private readonly ILogger<CreateBookingService> _logger;
    private readonly IProducer<string, BookingCreatedEvent> _kafkaProducer;

    public CreateBookingService(
        IBookingValidator validator,
        IBookingRepository repository,
        ILogger<CreateBookingService> logger,
        IProducer<string, BookingCreatedEvent> kafkaProducer)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
    }

    /// <summary>
    /// Creates a new booking after validation with the monolith service.
    /// </summary>
    /// <param name="request">Booking creation request with userId, hotelId, and optional promoCode</param>
    /// <returns>Booking response with booking details or error</returns>
    public async override Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation("Processing booking request for user {UserId} and hotel {HotelId}",
            request.UserId, request.HotelId);

        try
        {
            // Validate booking request with monolith service
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking validation failed: {Error}", validationResult.Error);
                return CreateErrorResponse(validationResult.Error);
            }

            // Calculate final price with discount
            var finalPrice = validationResult.BasePrice - validationResult.Discount;

            // Create booking entity
            var booking = new Models.Booking
            {
                UserId = request.UserId,
                HotelId = request.HotelId,
                PromoCode = request.PromoCode,
                DiscountPercent = validationResult.Discount,
                Price = finalPrice,
                CreatedAt = DateTime.UtcNow
            };

            // Persist booking to database
            await _repository.AddAsync(booking);
            await _repository.SaveChangesAsync();

            await _kafkaProducer.ProduceAsync("booking-creation", new Message<string, BookingCreatedEvent>() {
                Key = booking.Id.ToString(),
                Value = new BookingCreatedEvent
                {
                    UserId = booking.UserId,
                    HotelId = booking.HotelId,
                    PromoCode = booking.PromoCode,
                    DiscountPercent = booking.DiscountPercent,
                    Price = booking.Price,
                    CreatedAt = booking.CreatedAt
                }
            });

            _logger.LogInformation("Booking created successfully. BookingId={BookingId}, UserId={UserId}, FinalPrice={FinalPrice}",
                booking.Id, booking.UserId, booking.Price);

            return CreateSuccessResponse(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating booking");
            return CreateErrorResponse("An unexpected error occurred while creating the booking");
        }
    }

    public async override Task<BookingListResponse> ListBookings(BookingListRequest request, ServerCallContext context)
    {
        try
        {
            var bookings = await _repository.ListAsync(b => b.UserId == request.UserId.ToString());
            var response = new BookingListResponse();
            foreach (var booking in bookings)
            {
                response.Bookings.Add(new BookingResponse
                {
                    Id = booking.Id.ToString(),
                    UserId = booking.UserId,
                    HotelId = booking.HotelId,
                    PromoCode = booking.PromoCode,
                    DiscountPercent = booking.DiscountPercent,
                    Price = booking.Price,
                    CreatedAt = booking.CreatedAt?.ToString("O") // ISO-8601 format
                });
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing bookings for user {UserId}", request.UserId);
            return new BookingListResponse(); // Return empty list on error
        }
    }

    /// <summary>
    /// Generates a unique booking ID.
    /// </summary>
    private string GenerateBookingId()
    {
        return $"bk_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    /// <summary>
    /// Creates a successful booking response.
    /// </summary>
    private BookingResponse CreateSuccessResponse(Models.Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id.ToString(),
            UserId = booking.UserId,
            HotelId = booking.HotelId,
            PromoCode = booking.PromoCode,
            DiscountPercent = booking.DiscountPercent,
            Price = booking.Price,
            CreatedAt = booking.CreatedAt?.ToString("O") // ISO-8601 format
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    private BookingResponse CreateErrorResponse(string error)
    {
        return new BookingResponse
        {
            // Set only error-relevant fields
            // Note: In a real implementation, you might add an error field to the proto
        };
    }
}
