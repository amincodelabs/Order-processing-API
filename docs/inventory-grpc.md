# Inventory gRPC Service

The inventory gRPC service owns stock availability and reservation operations.

## Contract

The protobuf contract is defined in:

```text
src/InventoryGrpcService/Protos/inventory.proto
```

Service:

```proto
service Inventory {
  rpc CheckStock (CheckStockRequest) returns (CheckStockReply);
  rpc ReserveStock (ReserveStockRequest) returns (ReserveStockReply);
}
```

## Operations

`CheckStock` verifies whether a requested quantity is available for a product.

Request fields:

| Field | Type | Description |
| --- | --- | --- |
| `product_id` | `string` | Product identifier |
| `quantity` | `int32` | Requested quantity |

Response fields:

| Field | Type | Description |
| --- | --- | --- |
| `is_available` | `bool` | Whether the requested stock is available |
| `available_quantity` | `int32` | Current stock quantity |
| `message` | `string` | Human-readable result message |

`ReserveStock` reserves inventory by reducing the available quantity.

Request fields:

| Field | Type | Description |
| --- | --- | --- |
| `product_id` | `string` | Product identifier |
| `quantity` | `int32` | Quantity to reserve |

Response fields:

| Field | Type | Description |
| --- | --- | --- |
| `is_reserved` | `bool` | Whether reservation succeeded |
| `remaining_quantity` | `int32` | Remaining stock after reservation |
| `message` | `string` | Human-readable result message |

## Local Container

The service runs in Docker Compose as `inventory-grpc`.

```bash
docker compose up --build -d inventory-grpc
```

Host port:

```text
127.0.0.1:8081
```

Container port:

```text
8080
```

The service uses HTTP/2 because gRPC requires it.

## Persistence

The service stores inventory records in SQL Server through `InventoryDbContext`.

Schema changes are versioned under:

```text
src/InventoryGrpcService/Data/Migrations
```

On startup, the service applies pending EF Core migrations before accepting requests.

Unknown product IDs are initialized with a default quantity of `100` the first time they are checked or reserved.

The Order Processing API calls `ReserveStock` during order creation before it persists the order.

## Tests

Inventory reservation behavior is covered by:

```text
tests/InventoryGrpcService.Tests
```

The tests use SQLite in-memory storage so transaction behavior is exercised without requiring Docker or SQL Server.
