FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TradingBot.csproj", "TradingBot/"]
RUN dotnet restore "TradingBot/TradingBot.csproj"
WORKDIR "/src/TradingBot"
COPY . .
RUN dotnet build "TradingBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradingBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN mkdir -p "Data"
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradingBot.dll"]
