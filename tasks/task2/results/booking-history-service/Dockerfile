# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=builder /app/out .

EXPOSE 9090
ENV ASPNETCORE_URLS=http://+:9090

ENTRYPOINT ["dotnet", "booking-history-service.dll"]
