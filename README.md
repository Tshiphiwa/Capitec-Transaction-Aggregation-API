# Transaction Aggregation API

![CI](https://github.com/<your-username>/capiTransactionApi/actions/workflows/ci.yml/badge.svg)
[![codecov](https://codecov.io/gh/<your-username>/capiTransactionApi/branch/main/graph/badge.svg)](https://codecov.io/gh/<your-username>/capiTransactionApi)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-blue)
![Docker](https://img.shields.io/badge/docker-ready-2496ED)

A .NET 8 Web API that pulls transaction data from multiple bank source systems, categorises each transaction, and lets you query and aggregate the results.

## Why I chose this brief

I picked the Transaction Aggregation API over the other two options because it felt like the most foundational problem. A fraud engine needs clean, categorised transaction data to work. Account statements need aggregated history. I wanted to show I can build that data layer properly before anything else sits on top of it.

## Architecture

```
┌─────────────┐     POST /ingest      ┌──────────────────────┐
│   Client    │ ───────────────────▶  │  IngestionService    │
│  (Swagger)  │                       │  - fetch from sources│
└─────────────┘                       │  - deduplicate       │
       │                              │  - categorise        │
       │ GET /transactions            └──────────┬───────────┘
       │ GET /summary                            │
       ▼                                         ▼
┌─────────────────┐              ┌───────────────────────────┐
│ TransactionSvc  │◀─────────────│   CategorizationService   │
│ - filter        │              │   1. MCC code lookup      │
│ - paginate      │              │   2. Keyword fallback     │
│ - aggregate     │              │   3. Uncategorised        │
└────────┬────────┘              └───────────────────────────┘
         │
         ▼
┌─────────────────┐     ┌──────────────────────────────────┐
│   PostgreSQL    │     │         Mock Sources              │
│   (EF Core)     │     │  CARD (MCC) │ EFT │ WALLET (mix) │
└─────────────────┘     └──────────────────────────────────┘
```

## Getting started

You need Docker and Docker Compose installed.

```bash
git clone <repo-url>
cd Capitec-Transaction-Aggregation-API
cp .env.example .env
```

The `.env.example` file includes working defaults for the database. The only value you must change before running is `JWT_KEY`. Replace it with any random string of at least 32 characters.

```bash
docker compose up --build
```

Once it starts, open http://localhost:8080 in your browser. Swagger UI loads automatically.

## How to use it

**1. Log in**

```http
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "admin@capitec.com",
  "password": "Password123!"
}
```

Copy the `token` from the response.

**2. Authorise in Swagger**

Click the Authorize button at the top of the Swagger page, paste `Bearer <your-token>` and click Authorize. All requests you make from Swagger after that will include your token.

**3. Load some data**

```http
POST http://localhost:8080/api/transactions/ingest
Authorization: Bearer <your-token>
```

This fetches transactions from all three mock sources, categorises them and saves them to the database. Run it once after startup.

**4. Start querying**

```http
GET http://localhost:8080/api/transactions
GET http://localhost:8080/api/transactions?category=Dining
GET http://localhost:8080/api/transactions?sourceCode=CARD&from=2024-01-01
GET http://localhost:8080/api/transactions/summary
GET http://localhost:8080/api/transactions/aggregated
GET http://localhost:8080/api/transactions/{id}
GET http://localhost:8080/api/sources
GET http://localhost:8080/api/health
```

To override a category (Admin only):

```http
PATCH http://localhost:8080/api/transactions/{id}/category
Content-Type: application/json
{ "category": "Dining" }
```

## Running the tests

```bash
dotnet test
```

There are 72 unit tests covering MCC categorisation, keyword fallback, edge cases like null and empty inputs, role checks, not-found scenarios, JWT token generation, transaction mapping, source listing, ingestion deduplication, partial source failure, filter and pagination behaviour, and summary and aggregation calculations.

## How it works

**Project structure**

```
src/TransactionAggregationAPI/
    Controllers/      request handling
    Services/         business logic
    Models/           database entities
    DTOs/             request and response shapes
    Middleware/       error handling, correlation IDs
    MockSources/      simulated bank systems
    Infrastructure/   database context, MCC map, seeder
tests/
    TransactionAggregationApi.Tests/
```

I kept everything in one project rather than splitting into separate layers because the scope does not justify the extra overhead. The separation is still there though. Controllers do not touch the database. Services do not know about HTTP. If this grew into something bigger with multiple teams, I would split it up.

**How transactions get categorised**

Every card transaction comes with a 4-digit MCC code assigned by Visa or Mastercard. The first thing the categorisation service does is look that code up in a dictionary. If it finds a match, that is the category.

EFT transfers and salary payments do not have MCC codes. For those, the service scans the transaction description for keywords. "SALARY" maps to Income, "VODACOM" maps to Utilities, "RESTAURANT" maps to Dining, and so on.

If neither method finds a match, the transaction is marked Uncategorised. That is intentional. It is better to be honest about what we do not know than to guess wrong.

Every transaction also stores how its category was determined, whether that was an MCC lookup, a keyword match, a manual override, or left uncategorised. This makes the categorisation auditable and gives you the data you need to improve the keyword list over time.

**Mock data sources**

The three mock sources are internal controllers that return hardcoded data. The ingestion service calls them over HTTP the same way it would call a real downstream system. Swapping a mock for a real source means changing one URL in the database.

| Source | Code | Transactions | Has MCC codes |
|--------|------|-------------|---------------|
| Card Processing System | CARD | 10 | Yes |
| EFT Payment System | EFT | 14 | No |
| Digital Wallet System | WALLET | 8 | Mixed |

**Deduplication**

Each transaction is identified by its reference number plus which source it came from. Before saving anything, the ingestion service checks if that combination already exists. If it does, the transaction is skipped. You can call the ingest endpoint as many times as you want and it will never create duplicates.

## Design decisions

**Single project over layered architecture**

The scope is small enough that a separate Application, Domain and Infrastructure project would add navigation overhead without adding clarity. The boundaries are still enforced. Controllers never touch EF Core and services never reference HttpContext. They live in folders rather than assemblies. If this grew into a multi-team codebase I would split it.

**MCC-first, keyword fallback, then Uncategorised**

MCC codes are the most reliable signal because they are assigned by the card networks, not the merchant. Keywords are a reasonable fallback for EFT and salary transactions that have no MCC. Marking anything unmatched as Uncategorised rather than guessing keeps the data honest and gives an operator a clear queue to work through.

**Storing the categorisation method**

Every transaction records whether its category came from an MCC lookup, a keyword match, a manual override, or was left uncategorised. This makes the categorisation auditable and gives you the data you need to improve the keyword list over time.

**HTTP calls to mock sources instead of direct method calls**

The ingestion service calls mock sources over HTTP rather than calling their methods directly. This means swapping a mock for a real downstream system is a one-line config change, and the integration path is already tested.

**Offset pagination over cursor-based**

Offset pagination is simpler to implement and good enough for the data volumes here. At scale, with millions of transactions, I would switch to cursor-based pagination to avoid the performance cliff that comes with large offsets.

## Endpoints

| Method | Endpoint | Auth | What it does |
|--------|----------|------|--------------|
| POST | /api/auth/login | None | Log in and get a token |
| GET | /api/transactions | Required | Paginated list with filters |
| GET | /api/transactions/{id} | Required | Single transaction |
| GET | /api/transactions/summary | Required | Totals, net amount, spend by category |
| GET | /api/transactions/aggregated | Required | Grouped by category with percentages |
| PATCH | /api/transactions/{id}/category | Admin | Override a category |
| POST | /api/transactions/ingest | Admin | Pull data from all sources |
| GET | /api/sources | Required | All sources with transaction counts |
| GET | /api/health | None | Health check |

**Filter options for GET /api/transactions**

| Parameter | Type | Notes |
|-----------|------|-------|
| page | int | Defaults to 1 |
| pageSize | int | Defaults to 20, max 100 |
| category | string | e.g. Dining, Groceries |
| sourceCode | string | CARD, EFT or WALLET |
| transactionType | string | CardSwipe, EftTransfer, etc |
| direction | string | Debit or Credit |
| from | datetime | Start date |
| to | datetime | End date |
| minAmount | decimal | |
| maxAmount | decimal | |
| search | string | Searches description and merchant name |

## Environment variables

Copy `.env.example` to `.env` and fill in the values before running `docker compose up`.

| Variable | Required | Description |
|----------|----------|-------------|
| `DB_NAME` | Yes | PostgreSQL database name |
| `DB_USER` | Yes | PostgreSQL username |
| `DB_PASSWORD` | Yes | PostgreSQL password |
| `JWT_KEY` | Yes | Secret key used to sign JWT tokens. Use a long random string in production |

The `Jwt__Issuer` and `Jwt__Audience` values are set directly in `docker-compose.yml` and do not need to be in `.env`.

## What is already in place for production

- Structured logging with Serilog, rolling daily log files
- Every request gets a correlation ID that appears in all related log lines
- A global error handler catches every unhandled exception and returns clean JSON with no stack traces
- FluentValidation on all request inputs
- JWT authentication on every endpoint except health and login
- Admin and Analyst roles, category overrides are Admin only
- Database migrations run automatically on startup
- EF Core retries failed database connections up to 3 times
- Health check at /api/health returns 503 if the database is down
- Composite unique index on reference and source prevents duplicate ingestion
- Multi-stage Docker build, final image is around 200MB
- All secrets come from environment variables

## Licence

This project is licensed under the [MIT License](LICENSE).

## What I would add with more time

- Refresh tokens so users do not have to log in again after 8 hours
- Rate limiting on the ingest endpoint
- Cursor-based pagination instead of offset pagination for better performance at scale
- Integration tests that run against a real database
- OpenTelemetry for distributed tracing
- AWS deployment using ECS and Terraform, which matches what I do in my current role

## Default login

Email: admin@capitec.com  
Password: Password123!  
Role: Admin

The default password is hardcoded in `DatabaseSeeder.cs` and seeded on first startup for local development and demo convenience only. In a production deployment you would remove the seeder, provision the admin user through a secure bootstrap process, and rotate the password immediately. We never use this credential in any environment exposed to the internet.

## Tech stack

- .NET 8, ASP.NET Core Web API
- PostgreSQL 16, EF Core 8, Npgsql
- JWT authentication
- Serilog
- FluentValidation
- Swagger UI via Swashbuckle
- xUnit, Moq, FluentAssertions
- Docker, Docker Compose
