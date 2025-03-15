FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

WORKDIR /app

# Copy the published .NET app from a local build
COPY ./publish .

# Set execution permissions if needed
RUN chmod +x MyDotNetApp

# Run the application
CMD ["./MyDotNetApp"]
