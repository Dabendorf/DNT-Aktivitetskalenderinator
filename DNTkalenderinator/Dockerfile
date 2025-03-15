# Use the official .NET 9.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files into the container
COPY . .

# Restore dependencies
RUN dotnet restore

# Build the project and publish it to the /out directory
RUN dotnet publish -c Release -o /out

# Use the official .NET 9.0 runtime image (not ASP.NET) to run the application
FROM mcr.microsoft.com/dotnet/runtime:9.0

# Set the working directory
WORKDIR /app

# Copy the built application from the previous stage
COPY --from=build /out .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "DNTkalenderinator.dll"]