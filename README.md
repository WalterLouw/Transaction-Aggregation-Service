# Transaction Aggregation Service

A .NET 10 Web API that aggregates transactions from multiple data sources, categorises them automatically, and exposes the results via a versioned REST API. Background aggregation is driven by [Hangfire](https://www.hangfire.io/) on a configurable cron schedule, and incoming webhook events from external providers are validated using per-provider secrets.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the repository](#1-clone-the-repository)
  - [2. Configure the application](#2-configure-the-application)
  - [3. Run locally with dotnet](#3-run-locally-with-dotnet)
  - [4. Run with Docker](#4-run-with-docker)
- [Configuration Reference](#configuration-reference)
- [Development Mode](#development-mode)
- [Background Jobs](#background-jobs)
- [Transaction Categorisation](#transaction-categorisation)
- [Project Structure](#project-structure)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                  API Layer                       │
│   ASP.NET Core Web API  (versioned, Swagger)     │
└────────────────────┬────────────────────────────┘
                     │ MediatR
┌────────────────────▼────────────────────────────┐
│              Application Layer                   │
│   Command / Query Handlers, Business Logic       │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│            Infrastructure Layer                  │
│  Postgres (primary store)  ·  MSSQL (source DB)  │
│  External API  ·  Webhook receivers  ·  Hangfire │
└─────────────────────────────────────────────────┘
```

The service pulls transactions from:
- A **Microsoft SQL Server** source database
- A **third-party REST API**
- **Webhook callbacks** from external providers (HMAC-validated)

All ingested transactions are normalised, categorised by keyword rules, and persisted to **PostgreSQL**.

---

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [PostgreSQL](https://www.postgresql.org/) | 13+ |
| Microsoft SQL Server | 2019+ (or Azure SQL) |
| [Docker](https://www.docker.com/) *(optional)* | 20+ |

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/WalterLouw/Transaction-Aggregation-Service.git
cd Transaction-Aggregation-Service
```

### 2. Configure the application

Copy the example settings and fill in your values:

```bash
cp src/transaction-aggregator/appsettings.json src/transaction-aggregator/appsettings.Development.json
```

Open `appsettings.Development.json` and update the following (see [Configuration Reference](#configuration-reference) for all options):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=transaction_aggregator;Username=postgres;Password=yourpassword",
    "MssqlPrimary": "Server=localhost;Database=SourceDb;Trusted_Connection=True;"
  },
  "ExternalApi": {
    "BaseUrl": "https://api.thirdparty.com",
    "ApiKey": "your-api-key-here"
  },
  "Webhooks": {
    "Secrets": {
      "provider-a": "your-secret-here"
    }
  }
}
```

> **Note:** `appsettings.Development.json` is excluded from source control by `.gitignore`. Never commit real secrets.

### 3. Run locally with dotnet

```bash
cd src/transaction-aggregator
dotnet restore
dotnet run
```

The API will be available at:
- `http://localhost:5000` (HTTP)
- `https://localhost:5001` (HTTPS)
- `http://localhost:5000/swagger` — Swagger UI
- `http://localhost:5000/hangfire` — Hangfire dashboard *(development only)*

### 4. Run with Docker

Build the image:

```bash
docker build -t transaction-aggregator .
```

Run the container, passing configuration as environment variables:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=host.docker.internal;Database=transaction_aggregator;Username=postgres;Password=yourpassword" \
  -e ConnectionStrings__MssqlPrimary="Server=host.docker.internal;Database=SourceDb;Trusted_Connection=False;User Id=sa;Password=yourpassword" \
  -e ExternalApi__BaseUrl="https://api.thirdparty.com" \
  -e ExternalApi__ApiKey="your-api-key-here" \
  transaction-aggregator
```

> **Tip:** Use `host.docker.internal` to reach services running on your local machine from inside a Docker container (works on Docker Desktop for Mac/Windows; on Linux use `--network=host` instead).

---

## Configuration Reference

All settings live in `appsettings.json` and can be overridden per environment or via environment variables (using `__` as the hierarchy separator).

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:Postgres` | PostgreSQL connection string (primary store) | — |
| `ConnectionStrings:MssqlPrimary` | MSSQL source database connection string | — |
| `Hangfire:AggregationCron` | Cron expression for the aggregation job | `*/10 * * * *` (every 10 min) |
| `Hangfire:WorkerCount` | Number of Hangfire background workers | `2` |
| `Webhooks:Secrets:<provider>` | HMAC secret for each webhook provider | — |
| `ExternalApi:BaseUrl` | Base URL of the third-party transaction API | — |
| `ExternalApi:ApiKey` | API key for the third-party API | — |
| `ExternalApi:PageSize` | Number of records fetched per page | `100` |
| `Categorization:Rules` | Keyword-based categorisation rules (see below) | Built-in rules |

---

## Development Mode

When running in the `Development` environment, the service automatically swaps real data source connectors for **mock implementations**, so the API and Swagger UI work without needing live database or API connections.

The Hangfire dashboard is also only exposed in Development, at `/hangfire`.

To explicitly set the environment:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

---

## Background Jobs

Aggregation runs as a recurring Hangfire job named `aggregation-run`. The schedule is controlled by the `Hangfire:AggregationCron` setting in `appsettings.json`.

The default cron `*/10 * * * *` runs the job every 10 minutes. You can monitor, trigger, and inspect job history via the Hangfire dashboard at `/hangfire` in development.

---

## Transaction Categorisation

Transactions are automatically categorised based on keyword matching against the transaction description. Rules are evaluated in priority order and configured in `appsettings.json` under `Categorization:Rules`.

Each rule has:
- `Category` — the label applied to matching transactions
- `Priority` — lower number = higher priority (evaluated first)
- `Keywords` — list of case-insensitive substrings to match against

**Default categories:**

| Priority | Category | Example keywords |
|---|---|---|
| 1 | FoodAndDining | restaurant, cafe, coffee, pizza |
| 2 | Travel | airline, hotel, uber, airbnb |
| 3 | Entertainment | netflix, spotify, cinema, steam |
| 4 | Shopping | amazon, shop, retail, clothing |
| 5 | Utilities | electricity, internet, broadband |
| 6 | Healthcare | pharmacy, hospital, doctor, dental |

Custom rules can be added or existing ones modified directly in `appsettings.json`.

---

## Project Structure

```
Transaction-Aggregation-Service/
├── src/
│   ├── transaction-aggregator/   # API entry point (Program.cs, appsettings.json)
│   ├── Application/              # MediatR handlers, use cases
│   ├── Contracts/                # Request/response DTOs
│   └── Infrastructure/           # EF Core, data sources, Hangfire jobs, extensions
├── Dockerfile
├── .dockerignore
└── .gitignore
```
