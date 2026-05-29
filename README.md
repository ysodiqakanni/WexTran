# WexTran

A .NET 8 Web API for storing purchase transactions and retrieving them converted to foreign currencies using live exchange rates from the [U.S. Treasury Reporting Rates of Exchange](https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote)

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

### 2. Apply database migrations

```bash
dotnet ef database update --project WexTran.Api
```

### 3. Run the API

```bash
dotnet run --project WexTran.Api
```

The API starts on `http://localhost:5201`. Swagger UI is available at `http://localhost:5201/swagger`.

## Authentication

All endpoints require an `X-Api-Key` header. The development key is configured in `appsettings.json` under `ApiKey`.

**Using Swagger UI:** click the **Authorize** button (🔒) at the top of the Swagger page, enter the key, and confirm. Swagger will include the header on all subsequent requests.

**Using curl or any HTTP client:**
```
X-Api-Key: <your-api-key>
```

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
| `amountUsd` | Required, positive value; stored rounded to the nearest cent |

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
- `404` — transaction not found
- `422` — no exchange rate available within 6 months of the transaction date for the requested currency

## Health Check

```
GET /healthcheck
```

Returns `200 Healthy` or `503 Unhealthy` depending on database connectivity. Intended for container liveness/readiness probes.

## Running Tests

No SQL Server required — integration tests use an in-memory database.

```bash
dotnet test
```
