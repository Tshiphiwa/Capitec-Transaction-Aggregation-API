# Capitec Transaction Aggregation API

A production-oriented .NET 8 Web API designed for aggregating and categorizing customer transaction data from multiple mock financial sources. The solution is structured to reflect how a real-world backend service would be built and operated in a secure, maintainable, and deployable manner.

## Project brief

This repository implements the Transaction Aggregation API brief for Capitec.

## Solution overview

The API ingests transaction data from multiple mocked upstream systems, normalizes the payloads, assigns categories based on MCC and keyword rules, and exposes the data through a secure and queryable API surface. The service is designed for operational readiness and supports local development, automated testing, and Docker-based deployment.

## Architecture

The project follows a clean separation of concerns:

- Controllers: API endpoints for auth, health, sources, and transactions
- Services: business logic for categorization, ingestion, authentication, and transaction retrieval
- Infrastructure: EF Core persistence, migration support, and database seeding
- Models: domain entities such as transactions, users, and source metadata
- DTOs: request/response contracts for typed API communication
- Middleware: correlation IDs, centralized exception handling, and request tracing

## Tech stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT authentication
- Serilog structured logging
- Swagger / OpenAPI
- Docker / Docker Compose
- xUnit test suite

## Production-grade features included

- Secure configuration and secret management through environment variables
- JWT-based authentication and protected API access
- Database migrations for versioned schema changes
- Structured logging with correlation IDs for traceability
- Centralized exception handling for consistent API responses
- Health and readiness endpoints for dependency-aware monitoring
- Non-root container runtime for better runtime hygiene
- Restricted CORS configuration for controlled client access
- Dependency and image-level vulnerability scanning with Trivy by Aqua Security

## Prerequisites

- .NET 8 SDK
- Docker Desktop or Docker Engine
- PostgreSQL client tools (optional)

## Configuration

The repository keeps committed configuration placeholder-safe and does not store live secrets in source control. Local secrets should be stored in a local `.env` file.

1. Create the local environment file:

   ```bash
   cp .env.example .env
   ```

2. Add your local values:

   ```env
   DB_NAME=capitec_transactions
   DB_USER=postgres
   DB_PASSWORD=your_local_password
   JWT_KEY=replace_with_a_strong_secret_key
   ```

3. The runtime also reads placeholder-safe values from:
   - `src/TransactionAggregationAPI/appsettings.json`
   - `src/TransactionAggregationAPI/appsettings.Development.json`

## Run locally without Docker

From the repository root:

```bash
dotnet restore
dotnet ef database update --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj
dotnet run --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj
```

The API starts on the ASP.NET Core default local endpoints, typically:

```text
https://localhost:5001
http://localhost:5000
```

## Run with Docker Compose

From the repository root:

```bash
docker compose up --build
```

This starts:

- the API on port `8080`
- PostgreSQL on port `5432`

## Health and readiness checks

The service exposes health endpoints for runtime and dependency monitoring:

```http
GET /health
GET /ready
GET /api/health
```

## Authentication

The API supports JWT authentication.

### Login example

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Password123!"
}
```

## Main API endpoints

- `GET /api/health`
- `POST /api/auth/login`
- `GET /api/transactions`
- `GET /api/transactions/summary`
- `GET /api/sources`
- `POST /api/sources/ingest`

## Test execution

Run the full automated test suite:

```bash
dotnet test tests/TransactionAggregationApi.Tests/TransactionAggregationAPI.Tests.csproj --nologo
```

## Vulnerability scanning

As part of the production-grade security review, the container image was scanned with Trivy by Aqua Security to identify image and dependency vulnerabilities before final runtime use.

```bash
trivy image capitec-api-test:latest
```

This adds an additional security validation layer beyond unit tests and runtime health checks.

## Production readiness checklist

This project addresses the most relevant production-readiness practices for this brief:

- Health and readiness checks are in place for application and dependency health
- Structured logging with Serilog supports observability and incident investigation
- Correlation IDs are attached to each request for traceability
- Environment-based configuration keeps secrets out of source control
- EF Core migrations provide versioned database changes
- Docker runtime is hardened with a non-root user and health checks
- CORS is restricted to trusted origins
- Centralized exception handling keeps API failures predictable
- Security scanning is included through Trivy by Aqua Security

## Repository status

This project includes the required submission elements:

- runnable Dockerfile
- Docker Compose setup
- environment template file
- README with setup, run, and test instructions
- automated tests
- production-oriented architecture and security practices

## References used

- Production readiness guidance: https://medium.com/@soukainaguassmi/how-to-make-your-application-production-ready-a-practical-guide-81a7f50fad22
- Production-ready definition overview: https://www.mindstudio.ai/blog/what-does-production-ready-actually-mean
- Meaningful git commit messages: https://medium.com/@iambonitheuri/the-art-of-writing-meaningful-git-commit-messages-a56887a4cb49
- JWT authentication guidance: https://kristine-a-du.medium.com/step-by-step-guide-for-implementing-api-authentication-with-json-web-tokens-jwt-626f50449e4c
- Serilog guidance: https://elanchezhiyan-p.medium.com/mastering-serilog-in-net-the-complete-guide-2025-edition-fb9b0be855cb
