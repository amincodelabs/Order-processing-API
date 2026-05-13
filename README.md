# Order Processing API

A practical .NET Web API with REST endpoints, SQL Server persistence, Redis caching, Docker, and gRPC service-to-service communication.

The current version exposes REST APIs for products and orders, plus a separate inventory gRPC service. Products are stored in SQL Server and cached in Redis. Orders validate product stock, reduce inventory, and save order items.

## Run The App

Start the full development stack:

```bash
docker compose up --build -d
```

The API is available at:

```text
http://127.0.0.1:8080
```

Stop the stack:

```bash
docker compose down
```

Stop the stack and remove database/cache volumes:

```bash
docker compose down -v
```

## Containers

`docker-compose.yml` starts three services:

| Service | Purpose | Host Port |
| --- | --- | --- |
| `api` | .NET Order Processing API | `8080` |
| `inventory-grpc` | Inventory gRPC service | `8081` |
| `sqlserver` | SQL Server database | `1433` |
| `redis` | Redis cache | `6379` |

The API receives its database and Redis settings from environment variables in `docker-compose.yml`.

## Endpoints

Health check:

```bash
curl http://127.0.0.1:8080/health
```

List products:

```bash
curl http://127.0.0.1:8080/products
```

Create a product:

```bash
curl -X POST http://127.0.0.1:8080/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Stand",
    "sku": "STD-001",
    "price": 39.99,
    "stockQuantity": 20
  }'
```

Create an order:

```bash
curl -X POST http://127.0.0.1:8080/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Amin",
    "items": [
      {
        "productId": "replace-with-product-id",
        "quantity": 1
      }
    ]
  }'
```

Update order status:

```bash
curl -X PATCH "http://127.0.0.1:8080/orders/replace-with-order-id/status?status=Paid"
```

Valid statuses are:

```text
Pending, Paid, Packed, Shipped, Cancelled
```

## Current Behavior

On startup, the API:

1. Connects to SQL Server.
2. Applies pending EF Core migrations.
3. Seeds a few starter products if the database is empty.
4. Connects to Redis.

`GET /products` caches the product list in Redis for five minutes. Product creation and order creation clear that cache because both operations change product data or stock.

## Project Structure

```text
Contracts/                  Request DTOs accepted by API endpoints
Data/                       EF Core DbContext and database mapping
Data/Migrations/            Versioned SQL Server schema changes
Endpoints/                  Minimal API route groups
Models/                     Domain/data models
Services/                   Startup database seeding
src/InventoryGrpcService/   Inventory gRPC service
Program.cs                  Application composition and middleware setup
```

## Inventory gRPC Service

The inventory service defines its contract in `src/InventoryGrpcService/Protos/inventory.proto`.

Current operations:

- `CheckStock`
- `ReserveStock`

More detail: [docs/inventory-grpc.md](docs/inventory-grpc.md)

The REST API calls the gRPC inventory service during order creation before it persists the order.
