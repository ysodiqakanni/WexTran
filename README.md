# WexTran

A .NET 8 Web API for storing purchase transactions and retrieving them converted to foreign currencies using live exchange rates from the [U.S. Treasury Reporting Rates of Exchange](https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange).

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Running with Docker Compose](#running-with-docker-compose)
- [Authentication](#authentication)
- [API Endpoints](#api-endpoints)
- [Health Check](#health-check)
- [Running Tests](#running-tests)
- [Architecture and Design Decisions](#architecture-and-design-decisions)

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote)

---

## Getting Started

### 1. Configure the database connection

Open `WexTran.Api/appsettings.json` and update the connection string to point to your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=localhost;Initial Catalog=WexTransactions;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;"
}
```

Alternatively, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep credentials out of source control:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>" --project WexTran.Api
```

### 2. Run the API

Migrations are applied automatically on startup.

```bash
dotnet run --project WexTran.Api
```

The API starts on `http://localhost:5201`. Swagger UI is available at `http://localhost:5201/swagger`.

---

## Running with Docker Compose

The compose file runs the API container only. It expects a remote SQL Server instance to already exist and be accessible from the container.

Set the required environment variables before starting, either by exporting them in your shell or by creating a `.env` file in the project root:

```env
DB_CONNECTION_STRING=Server=<host>;Database=WexTransactions;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;
API_KEY=<your-api-key>
```

Then run:

```bash
docker-compose up --build
```

The API will be available at `http://localhost:8080` and Swagger UI at `http://localhost:8080/swagger`.

> **Note:** Ensure database migrations have been applied to the remote SQL Server before starting the container. See [Applying Migrations](#applying-migrations) below.

To stop the container:

```bash
docker-compose down
```

### Applying Migrations

Migrations are **not** applied automatically on startup. Run them as a deliberate step before deploying a new version:

```bash
dotnet ef database update --project WexTran.Api
```

In a CI/CD pipeline, this should run as a pre-deploy step against the target database.

---

## Authentication

All endpoints require an `X-Api-Key` header. The development key is configured in `appsettings.json` under `ApiKey`.

**Using Swagger UI:** click the **Authorize** button (🔒) at the top of the page, enter the API key, and confirm. Swagger will include the header on every subsequent request automatically.

**Using curl or any HTTP client:**

```
X-Api-Key: <your-api-key>
```

---

## API Endpoints

### `POST /api/transactions`

Stores a new purchase transaction.

**Request body:**
```json
{
  "description": "Hotel stay",
  "transactionDate": "2024-03-15",
  "amountUsd": 250.00
}
```

| Field | Constraints |
|---|---|
| `description` | Required, max 50 characters |
| `transactionDate` | Required, valid date |
| `amountUsd` | Required, between $0.01 and $10,000,000; stored rounded to the nearest cent |

**Response `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Hotel stay",
  "transactionDate": "2024-03-15T00:00:00",
  "amountUsd": 250.00
}
```

---

### `GET /api/transactions/{id}?currency={currency}`

Retrieves a stored transaction converted to the specified currency using the Treasury exchange rate active on or before the transaction date (within the last 6 months).

**Currency format:** the `currency` parameter must match the `country_currency_desc` field in the Treasury dataset, for example:

| Currency | Value to pass |
|---|---|
| Canadian Dollar | `Canada-Dollar` |
| Mexican Peso | `Mexico-Peso` |
| Euro | `Euro Zone-Euro` |
| British Pound | `United Kingdom-Pound` |
| Japanese Yen | `Japan-Yen` |

A full list is available on the [Treasury dataset page](https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange).

**Response `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Hotel stay",
  "transactionDate": "2024-03-15T00:00:00",
  "amountUsd": 250.00,
  "currency": "Canada-Dollar",
  "exchangeRate": 1.3500,
  "convertedAmount": 337.50
}
```

**Error responses:**
- `400`: invalid or missing request parameters
- `401`: missing or invalid API key
- `404`: transaction not found
- `422`: no exchange rate available within 6 months of the transaction date for the requested currency

---

## Health Check

```
GET /healthcheck
```

Returns `200 Healthy` or `503 Unhealthy` depending on database connectivity. Intended for container liveness and readiness probes.

---

## Running Tests

No SQL Server required; integration tests use an in-memory database.

```bash
dotnet test
```

---

## Architecture and Design Decisions

### Layered MVC Architecture

The project follows a strict three-layer separation:

- **Controllers** handle HTTP concerns only: deserialising the request, invoking the service, and returning the response. They contain no business logic.
- **Services** own all business logic: validation rules, rounding, and orchestrating calls to the repository and external exchange rate service.
- **Repositories** abstract data access behind an interface. This decouples the service layer from Entity Framework Core, making the business logic independently testable with mocks.

This separation keeps each layer focused, independently testable, and easy to change without affecting the others.

---

### Exchange Rate Caching

Exchange rates are fetched from the Treasury API via `TreasuryExchangeRateService`. This is wrapped by `CachedExchangeRateService` using the decorator pattern, which caches results in-memory for 24 hours keyed on `{currency}:{date}`.

**Why:** The Treasury dataset is updated quarterly, so the same currency/date pair will always return the same rate. Caching eliminates redundant external API calls, reduces latency on repeated lookups, and protects against rate limiting or temporary Treasury API outages.

---

### Retry Policy and Circuit Breaker

HTTP calls to the Treasury API are protected by a resilience pipeline via `Microsoft.Extensions.Http.Resilience`:

- **Retry:** up to 3 attempts with exponential backoff and jitter, reducing thundering herd effects during transient failures.
- **Circuit breaker:** opens after 50% of requests fail within a 30-second sampling window (minimum 5 requests). Once open, it stays open for 15 seconds before allowing a test request through.
- **Timeout:** each individual attempt times out after 10 seconds.

**Why:** The Treasury API is an external dependency outside our control. Without these policies, a slow or unavailable external service would cause threads to pile up and cascade into full API failures. The circuit breaker prevents the system from repeatedly hammering a failing downstream service, giving it time to recover.

---

### Logging

ASP.NET Core's built-in `ILogger` is used throughout. The `GlobalExceptionHandler` logs:

- Unhandled exceptions at `Error` level with full stack traces, covering database failures, unexpected nulls, and any other unhandled fault.
- Known business exceptions (`TransactionNotFoundException`, `CurrencyConversionUnavailableException`, etc.) at `Warning` level, confirming the system handled the case correctly without the noise of a stack trace.

**In a production environment,** `ILogger` would be backed by [Serilog](https://serilog.net/) configured to write structured JSON logs to a rolling file sink and ship to an observability platform such as [Datadog](https://www.datadoghq.com/) or [New Relic](https://newrelic.com/). This enables log aggregation, alerting, dashboards, and distributed tracing across services, none of which are available from console output alone.

---

### Transaction Amount Cap ($10,000,000)

The `amountUsd` field is validated to a maximum of $10,000,000 per transaction.

**Why:** A purchase transaction API has no legitimate use case for amounts beyond this threshold. The cap prevents accidental data entry errors (e.g., submitting `250000000` instead of `250.00`) from polluting the dataset and producing misleading currency conversions. It also serves as a basic safeguard against malformed or malicious input.

---

### Custom Exceptions and Global Exception Handler

Domain-specific exceptions (`InvalidTransactionException`, `TransactionNotFoundException`, `CurrencyConversionUnavailableException`) are thrown from the service layer and caught centrally by `GlobalExceptionHandler`, which maps each type to the appropriate HTTP status code and returns an [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) `ProblemDetails` response.

**Why:** Without a central handler, each controller action would need its own try/catch blocks to produce consistent error responses, a violation of DRY and a frequent source of inconsistent error shapes across endpoints. The global handler keeps controllers thin, guarantees a uniform error response format, and ensures all exceptions are logged in one place regardless of which endpoint triggered them.
