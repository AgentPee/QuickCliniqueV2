# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy NuGet.config if it exists (for package source configuration)
COPY ["NuGet.config", "."]

# Copy csproj file first for better layer caching
COPY ["QuickClinique/QuickClinique.csproj", "QuickClinique/"]

# Copy everything else (excluding bin/obj folders)
COPY QuickClinique/ QuickClinique/

# Set working directory
WORKDIR /src/QuickClinique

# Clean any existing build artifacts
RUN rm -rf bin obj

# Restore and build in a single command to ensure packages persist
# This prevents package resolution issues between restore and build steps
RUN dotnet restore "QuickClinique.csproj" && \
    dotnet build "QuickClinique.csproj" -c Release -o /app/build

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

