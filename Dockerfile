FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files first to maximize Docker layer caching.
COPY ["src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj", "src/TransactionAggregationAPI/"]
COPY ["tests/TransactionAggregationApi.Tests/TransactionAggregationAPI.Tests.csproj", "tests/TransactionAggregationApi.Tests/"]

RUN dotnet restore "src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj"

COPY . .

RUN dotnet publish "src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p logs && \
    chown -R app:app /app

COPY --from=build /app/publish .

RUN chown -R app:app /app && \
    chmod -R 755 /app

USER app

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "Capitec-Transaction-Aggregation-API.dll"]