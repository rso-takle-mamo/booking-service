FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookingService.DatabaseMigrator/BookingService.DatabaseMigrator.csproj", "BookingService.DatabaseMigrator/"]
RUN dotnet restore "BookingService.DatabaseMigrator/BookingService.DatabaseMigrator.csproj"
COPY . .
WORKDIR "/src/BookingService.DatabaseMigrator"
RUN dotnet build "./BookingService.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookingService.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.DatabaseMigrator.dll"]
