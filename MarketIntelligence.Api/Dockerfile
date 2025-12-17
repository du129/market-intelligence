# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["MarketIntelligence.Api/MarketIntelligence.Api.csproj", "MarketIntelligence.Api/"]
COPY ["MarketIntelligence.Shared/MarketIntelligence.Shared.csproj", "MarketIntelligence.Shared/"]
RUN dotnet restore "MarketIntelligence.Api/MarketIntelligence.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MarketIntelligence.Api"
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarketIntelligence.Api.dll"]