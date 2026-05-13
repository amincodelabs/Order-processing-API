# Architecture Notes

This service uses a small set of backend components that can evolve independently as the application grows.

## Request Flow

For a product list request:

1. The client calls `GET /products`.
2. `ProductEndpoints` checks Redis for the cached product list.
3. If Redis has data, the API returns it.
4. If Redis has no data, the API reads SQL Server through EF Core.
5. The API stores the result in Redis and returns it.

For an order creation request:

1. The client calls `POST /orders`.
2. `OrderEndpoints` validates customer and item data.
3. The API loads requested products from SQL Server.
4. The API checks stock quantity.
5. The API reduces product stock and saves the order plus order items.
6. The API clears the product-list cache because stock changed.

## Main Components

`Program.cs` wires the application together. It registers EF Core, Redis, OpenAPI, endpoint groups, and startup seeding.

`OrderProcessingDbContext` is the EF Core database context. It defines the database tables and important constraints, such as unique product SKUs and decimal precision for money values.

`ProductEndpoints` owns product-related HTTP routes. It also owns the Redis cache key for product lists.

`OrderEndpoints` owns order-related HTTP routes. For now, it performs stock validation directly. Later, this logic will move behind a gRPC inventory service.

`DatabaseSeeder` creates the database schema and adds starter products for local development.

`InventoryGrpcService` owns inventory-related gRPC operations. It exposes stock-checking and stock-reservation RPCs through the `inventory.proto` contract.

## Why SQL Server

SQL Server is used for durable business data:

- products
- stock quantity
- orders
- order items

This is data we do not want to lose when the app restarts.

## Why Redis

Redis is used for fast, temporary data. In this version, it caches `GET /products` responses.

Redis is a good fit here because product listing is read often, and stale data for a short time is acceptable. When product data or stock changes, the API deletes the cache entry.

## Current Limitations

The project currently uses `EnsureCreatedAsync` instead of EF Core migrations. Migrations should replace it when the schema starts evolving.

The API currently checks inventory inside the order endpoint. That couples order creation to inventory rules. The next implementation step is to route inventory checks through the gRPC service.
