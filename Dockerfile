# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj file and restore dependencies
COPY ["QuickClinique/QuickClinique.csproj", "QuickClinique/"]
RUN dotnet restore "QuickClinique/QuickClinique.csproj"

# Copy everything else and build
COPY QuickClinique/ QuickClinique/
WORKDIR /src/QuickClinique
RUN dotnet build "QuickClinique.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
WORKDIR /src/QuickClinique
RUN dotnet publish "QuickClinique.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
# Railway will set PORT environment variable, we'll use it in the entrypoint
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Expose port (Railway will use the PORT env var)
EXPOSE 8080

# Run the app
# Railway sets PORT automatically, but we'll use 8080 as fallback
ENTRYPOINT ["dotnet", "QuickClinique.dll"]

