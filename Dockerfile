FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# We must copy project files to cache to restore the layer first
COPY ["src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj", "src/TransactionAggregationAPI/"]
COPY ["src/TransactionAggregationAPI.Tests/Capitec-Transaction-Aggregation-API.Tests.csproj", "tests/TransactionAggregationAPI.Tests/"]

RUN dotnet restore "src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj"

COPY . .

RUN dotnet publish "src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj" -c Release -o /app/publish /--no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

RUN mkdir -p logs

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Capitec-Transaction-Aggregation-API.dll"]