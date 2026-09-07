# Capitec Transaction Aggregation API

> A production-oriented .NET 8 Web API for aggregating and categorizing customer transaction data from multiple financial sources. Built with clean architecture, security, and operational readiness in mind.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Testing](#testing)
- [Docker Deployment](#docker-deployment)
- [Health Checks](#health-checks)
- [Security](#security)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

The **Capitec Transaction Aggregation API** is a .NET 8 Web API that ingests transaction data from multiple mocked upstream financial systems, normalizes payloads, assigns categories based on MCC codes and keyword rules, and exposes the data through a secure, queryable API surface.

### Problem Statement

Capitec needs a unified view of customer transactions across disparate systems (EFT, card payments, digital wallets). This API solves that by providing:
- **Multi-source ingestion** from mocked EFT, card, and wallet transaction systems
- **Automatic categorization** using MCC codes and merchant keyword matching
- **Secure, authenticated access** with JWT tokens
- **Production-ready observability** with structured logging, correlation IDs, and health checks

---

## Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      Controllers                            │
│  Auth │ Health │ Sources │ Transactions                      │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                       Services                              │
│  Auth │ Categorization │ Ingestion │ Transaction            │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure                           │
│  EF Core │ PostgreSQL │ Migrations │ Seeding                │
└─────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Components | Responsibility |
|-------|------------|----------------|
| **Presentation** | Controllers, Middleware | HTTP handling, request/response serialization, correlation IDs, exception handling |
| **Application** | Services, DTOs | Business logic, categorization rules, ingestion orchestration, transaction queries |
| **Domain** | Models, Enums | Core entities (Transaction, User, TransactionSource), business rules |
| **Infrastructure** | EF Core, Repositories | Data persistence, migrations, database seeding |

---

## Tech Stack

| Category | Technology | Version |
|----------|------------|---------|
| **Runtime** | .NET | 8.0 LTS |
| **Framework** | ASP.NET Core | 8.0 |
| **Database** | PostgreSQL | 15 |
| **ORM** | Entity Framework Core | 8.0 |
| **Auth** | JWT Bearer Tokens | - |
| **Logging** | Serilog | 3.x |
| **API Docs** | Swagger/OpenAPI | - |
| **Containerization** | Docker & Docker Compose | - |
| **Testing** | xUnit, Moq, FluentAssertions | - |
| **Security Scan** | Trivy (Aqua Security) | - |

---

## Features

### Core Functionality
- ✅ **Multi-source transaction ingestion** (EFT, Card, Wallet mock sources)
- ✅ **Automatic categorization** via MCC codes + merchant keyword rules
- ✅ **Paginated, filterable transaction queries** (date range, source, category, amount)
- ✅ **Transaction summaries** with category breakdowns
- ✅ **Source management** (list configured sources, trigger ingestion)

### Security & Auth
- ✅ **JWT-based authentication** with secure key management
- ✅ **Protected endpoints** requiring valid Bearer tokens
- ✅ **Environment-based secrets** (no secrets in source control)
- ✅ **Restricted CORS** for controlled client access

### Observability & Operations
- ✅ **Structured logging** with Serilog (JSON format)
- ✅ **Correlation IDs** on every request for distributed tracing
- ✅ **Health & readiness endpoints** (`/health`, `/ready`)
- ✅ **Centralized exception handling** with consistent error responses
- ✅ **Non-root Docker container** for runtime security

### Quality Assurance
- ✅ **Automated test suite** (unit + integration tests)
- ✅ **Database migrations** for versioned schema changes
- ✅ **Vulnerability scanning** with Trivy
- ✅ **Code analysis** via `dotnet format` / analyzers

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Docker Desktop / Engine | 24.0+ | [Download](https://www.docker.com/products/docker-desktop/) |
| PostgreSQL Client (optional) | 15+ | [Download](https://www.postgresql.org/download/) |
| Git | 2.40+ | [Download](https://git-scm.com/) |

---

## Quick Start

### Option 1: Docker Compose (Recommended)

```bash
# 1. Clone the repository
git clone <repository-url>
cd Capitec-Transaction-Aggregation-API

# 2. Create environment file from template
cp .env.example .env

# 3. Edit .env with your values (see Configuration section)
# 4. Start the stack
docker compose up --build
```

**Access points:**
- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

---

### Option 2: Local Development (No Docker)

```bash
# 1. Restore dependencies
dotnet restore

# 2. Create and configure .env file
cp .env.example .env
# Edit .env with your local PostgreSQL connection details

# 3. Apply database migrations
dotnet ef database update --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj

# 4. Run the API
dotnet run --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj
```

**Access points:**
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

---

## Configuration

The application uses **environment variables** for secrets and **appsettings.json** for non-sensitive configuration.

### Environment Variables (`.env`)

Create from template:
```bash
cp .env.example .env
```

Required variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `DB_NAME` | PostgreSQL database name | `capitec_transactions` |
| `DB_USER` | Database username | `postgres` |
| `DB_PASSWORD` | Database password | `secure_password_123` |
| `DB_HOST` | Database host (Docker: `db`, Local: `localhost`) | `db` |
| `DB_PORT` | Database port | `5432` |
| `JWT_KEY` | Signing key (min 256 bits) | `your-super-secret-key-at-least-32-chars` |
| `JWT_ISSUER` | Token issuer | `CapitecTransactionAPI` |
| `JWT_AUDIENCE` | Token audience | `CapitecTransactionAPI` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` / `Production` |

### App Settings (`appsettings.json`)

Non-sensitive configuration:
- Connection string template (placeholders replaced at runtime)
- Serilog logging levels
- CORS allowed origins
- Swagger configuration

---

## Running the Application

### Development Mode

```bash
# With hot reload
dotnet watch run --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj
```

### Production Build

```bash
# Build optimized release
dotnet publish src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj -c Release -o ./publish

# Build Docker image
docker build -t capitec-transaction-api:latest .
```

### Database Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj

# Apply migrations
dotnet ef database update --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj

# Remove last migration
dotnet ef migrations remove --project src/TransactionAggregationAPI/Capitec-Transaction-Aggregation-API.csproj
```

---

## API Documentation

### Swagger / OpenAPI

Interactive API documentation is available at:
- **Local**: `https://localhost:5001/swagger`
- **Docker**: `http://localhost:8080/swagger`

### Base URL

| Environment | Base URL |
|-------------|----------|
| Local (HTTPS) | `https://localhost:5001` |
| Local (HTTP) | `http://localhost:5000` |
| Docker | `http://localhost:8080` |

### Endpoints Overview

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `GET` | `/health` | Liveness probe | ❌ |
| `GET` | `/ready` | Readiness probe (checks DB) | ❌ |
| `GET` | `/api/health` | Detailed health info | ❌ |
| `POST` | `/api/auth/login` | Obtain JWT token | ❌ |
| `GET` | `/api/transactions` | List transactions (paginated) | ✅ |
| `GET` | `/api/transactions/{id}` | Get transaction by ID | ✅ |
| `GET` | `/api/transactions/summary` | Category breakdown summary | ✅ |
| `GET` | `/api/sources` | List configured sources | ✅ |
| `POST` | `/api/sources/ingest` | Trigger ingestion from all sources | ✅ |

---

## Authentication

The API uses **JWT Bearer tokens**. Include the token in the `Authorization` header:

```
Authorization: Bearer <your-jwt-token>
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-09-07T15:30:00Z"
}
```

### Default Test Credentials

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Password123!` | Administrator |

> **Note**: In production, use strong passwords and rotate the `JWT_KEY` regularly.

---

## Testing

### Run All Tests

```bash
# From repository root
dotnet test --nologo
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Test Categories

| Test Project | Description |
|--------------|-------------|
| `TransactionAggregationApi.Tests` | Unit & integration tests for services, controllers, middleware |

### Test Environment

Tests run under the `Testing` environment using **EF Core In-Memory provider** (no PostgreSQL required).

---

## Docker Deployment

### Run Production Stack

```bash
docker compose -f docker-compose.yml up --build -d
```

### View Logs

```bash
docker compose logs -f api
```

### Security Scanning

```bash
# Scan built image for vulnerabilities
trivy image capitec-transaction-api:latest

# Scan with severity filter
trivy image --severity HIGH,CRITICAL capitec-transaction-api:latest
```

---

## Health Checks

| Endpoint | Purpose | Dependencies Checked |
|----------|---------|---------------------|
| `GET /health` | Liveness (k8s livenessProbe) | None - process alive |
| `GET /ready` | Readiness (k8s readinessProbe) | Database connectivity |
| `GET /api/health` | Detailed status | DB, configuration, uptime |

### Example Response

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "description": "PostgreSQL connection successful"
    }
  ],
  "totalDuration": "00:00:00.045"
}
```

---

## Security

### Implemented Measures

| Measure | Implementation |
|---------|----------------|
| **Secret Management** | Environment variables only, `.env` in `.gitignore` |
| **Authentication** | JWT with HS256, configurable expiry |
| **Authorization** | `[Authorize]` attribute on protected endpoints |
| **CORS** | Restricted to configured origins only |
| **Container** | Non-root user (`appuser`), read-only filesystem |
| **Headers** | Security headers via middleware |
| **Scanning** | Trivy vulnerability scanning in CI/CD |

### Security Checklist for Production

- [ ] Rotate `JWT_KEY` to a strong 256-bit secret
- [ ] Use managed PostgreSQL (Azure Database, AWS RDS, etc.)
- [ ] Enable TLS/SSL termination at reverse proxy
- [ ] Configure rate limiting
- [ ] Set up audit logging for auth events
- [ ] Run Trivy scan on every build
- [ ] Review and restrict CORS origins

---

## Project Structure

```
Capitec-Transaction-Aggregation-API/
├── .github/                    # GitHub Actions workflows (if any)
├── src/
│   └── TransactionAggregationAPI/
│       ├── Controllers/        # API endpoints
│       │   ├── AuthController.cs
│       │   ├── HealthController.cs
│       │   ├── SourcesController.cs
│       │   ├── TransactionController.cs
│       │   └── MockSources/    # Mock upstream systems
│       ├── Services/           # Business logic
│       │   ├── AuthService.cs
│       │   ├── CategorizationService.cs
│       │   ├── IngestionService.cs
│       │   └── TransactionService.cs
│       ├── Models/             # Domain entities
│       │   ├── Transaction.cs
│       │   ├── User.cs
│       │   ├── TransactionSource.cs
│       │   └── Enums.cs
│       ├── DTOs/               # Request/Response contracts
│       ├── Infrastructure/     # EF Core, Repositories
│       │   ├── Data/
│       │   ├── Migrations/
│       │   └── Repositories/
│       ├── Middleware/         # Cross-cutting concerns
│       │   ├── CorrelationIdMiddleware.cs
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Extensions/         # DI registration
│       ├── Properties/
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Local.json
│       ├── Program.cs
│       └── Dockerfile
├── tests/
│   └── TransactionAggregationApi.Tests/
├── docker-compose.yml
├── .env.example
├── .dockerignore
├── .gitignore
├── TransactionAggregationAPI.sln
└── README.md
```

---

## References used

- [Production Readiness Guide](https://medium.com/@soukainaguassmi/how-to-make-your-application-production-ready-a-practical-guide-81a7f50fad22)
- [What Does Production Ready Actually Mean?](https://www.mindstudio.ai/blog/what-does-production-ready-actually-mean)
- [Meaningful Git Commit Messages](https://medium.com/@iambonitheuri/the-art-of-writing-meaningful-git-commit-messages-a56887a4cb49)
- [JWT Authentication Guide](https://kristine-a-du.medium.com/step-by-step-guide-for-implementing-api-authentication-with-json-web-tokens-jwt-626f50449e4c)
- [Serilog Complete Guide](https://elanchezhiyan-p.medium.com/mastering-serilog-in-net-the-complete-guide-2025-edition-fb9b0be855cb)

---