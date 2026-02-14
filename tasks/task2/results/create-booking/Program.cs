using create_booking.Extensions;
using create_booking.Services;
using Grpc.AspNetCore.Server;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddBookingServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<CreateBookingService>();
// Reflection доступен только в Development
app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
