# Use the official .NET 9.0 SDK image to build the application
# This will be compatible with both AMD64 and ARM64
FROM --platform=linux/amd64,linux/arm64 mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files into the container
COPY . .

# Restore dependencies
RUN dotnet restore

# Build the project
RUN dotnet publish -c Release -o /out

# Use the official .NET 9.0 runtime image to run the application
# Again, this ensures the runtime is compatible with both platforms
FROM --platform=linux/amd64,linux/arm64 mcr.microsoft.com/dotnet/aspnet:9.0

# Set the working directory
WORKDIR /app

# Copy the built application from the previous stage
COPY --from=build /out .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "DNTkalenderinator.dll"]