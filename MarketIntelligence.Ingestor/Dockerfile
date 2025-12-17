# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MarketIntelligence.Ingestor/MarketIntelligence.Ingestor.csproj", "MarketIntelligence.Ingestor/"]
COPY ["MarketIntelligence.Shared/MarketIntelligence.Shared.csproj", "MarketIntelligence.Shared/"]
RUN dotnet restore "MarketIntelligence.Ingestor/MarketIntelligence.Ingestor.csproj"

COPY . .
WORKDIR "/src/MarketIntelligence.Ingestor"
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

# --- OPTIMIZATION START ---
# Install Chromium FIRST. 
# Docker will CACHE this layer. It will only run once, not every time you change C# code.
RUN apt-get update && apt-get install -y chromium && rm -rf /var/lib/apt/lists/*
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
# --- OPTIMIZATION END ---

WORKDIR /app

# Copy the app code AFTER installing dependencies
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MarketIntelligence.Ingestor.dll"]