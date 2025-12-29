# Booking Service

## Overview

The Booking Service manages appointment bookings, customer reservations, and booking status tracking for the appointments system. It integrates with the Availability Service via gRPC to ensure bookings are only created for available time slots.

## Database

### Tables and Schema

#### Bookings Table
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Booking identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `OwnerId` | UUID | Required | Customer who made the booking |
| `ServiceId` | UUID | Required, Foreign Key | Reference to service being booked |
| `StartDateTime` | TIMESTAMPTZ | Required | Start time of the booking |
| `EndDateTime` | TIMESTAMPTZ | Required | End time of the booking |
| `BookingStatus` | INTEGER | Required | Status of the booking (0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled) |
| `Notes` | VARCHAR(1000) | Nullable | Optional notes for the booking |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### Services Table
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Service identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `Name` | VARCHAR(255) | Required | Service name |
| `Description` | VARCHAR(1000) | Nullable | Service description |
| `Price` | DECIMAL(10,2) | Required | Service price |
| `DurationMinutes` | INTEGER | Required | Service duration in minutes |
| `CategoryId` | UUID | Nullable, Foreign Key | Reference to category |
| `IsActive` | BOOLEAN | Required | Whether the service is active for booking (default: true) |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### Categories Table
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Category identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `Name` | VARCHAR(255) | Required | Category name |
| `Description` | VARCHAR(1000) | Nullable | Category description |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### Tenants Table
**NOTE:** This table is shared across services and synchronized via event streaming (Kafka/RabbitMQ).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Tenant identifier |
| `BusinessName` | VARCHAR(255) | Required | Business name |
| `Email` | VARCHAR(255) | Nullable | Business email |
| `Phone` | VARCHAR(50) | Nullable | Business phone |
| `Address` | VARCHAR(500) | Nullable | Business address |
| `TimeZone` | VARCHAR(50) | Nullable | Time zone for the tenant |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

### Database Relationships
1. **Bookings → Tenants:** Many-to-one via `TenantId`
2. **Bookings → Customers:** Many-to-one via `OwnerId`
3. **Bookings → Services:** Many-to-one via `ServiceId`
4. **Services → Categories:** Many-to-one via `CategoryId`
5. **Categories → Tenants:** Many-to-one via `TenantId`

### Foreign Key Constraints
- `FK_Bookings_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)
- `FK_Bookings_Services_ServiceId` → `Services(Id)` (ON DELETE RESTRICT)
- `FK_Services_Categories_CategoryId` → `Categories(Id)` (ON DELETE SET NULL)
- `FK_Services_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)
- `FK_Categories_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)

## API Endpoints

### Booking Endpoints (`/api/bookings`)

#### Create Booking
```http
POST /api/bookings
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Create a new booking

**Remarks:**
- **CUSTOMERS ONLY:** Only customers can create bookings
- Must provide `tenantId` in request body to specify which tenant's service to book
- The end time is automatically calculated based on service duration
- Service must be active (`IsActive = true`)
- Service must belong to the specified tenant
- Time slot availability is checked via gRPC call to Availability Service

**Request Body:**
```json
{
  "serviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startDateTime": "2025-12-10T10:00:00Z",
  "tenantId": "456e7890-e89b-12d3-a456-426614174001",
  "notes": "First time visit"
}
```

**Response:** Created booking details with location header pointing to `GET /api/bookings/{id}`

#### Get Booking by ID
```http
GET /api/bookings/{id}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get a specific booking by ID

**Remarks:**
- **CUSTOMERS:** Can only retrieve their own bookings
- **PROVIDERS:** Can retrieve any booking within their tenant

**Parameters:**
- `id` (GUID, required) - Booking ID

**Response:** Booking details

#### Get Bookings (List/Filter)
```http
GET /api/bookings?offset={integer}&limit={integer}&tenantId={guid}&startDate={datetime}&endDate={datetime}&status={integer}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get paginated list of bookings with filtering

**Remarks:**
- **CUSTOMERS:**
  - Must provide `tenantId` query parameter
  - Can only retrieve their own bookings
- **PROVIDERS:**
  - Cannot provide `tenantId` parameter (automatically uses their own tenant)
  - Can retrieve all bookings within their tenant

**Parameters:**
- `offset` (Integer, optional) - Number of items to skip (default: 0)
- `limit` (Integer, optional) - Number of items to return (default: 50)
- `tenantId` (GUID, required for customers, forbidden for providers)
- `startDate` (DateTime, optional) - Filter bookings from this date onwards (format: 2026-01-01T00:00:00Z)
- `endDate` (DateTime, optional) - Filter bookings up to this date (format: 2026-01-01T23:59:59Z)
- `status` (Integer, optional) - Filter by booking status (0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled)

**Response:**
```json
{
  "offset": 0,
  "limit": 50,
  "totalCount": 1,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "tenantId": "456e7890-e89b-12d3-a456-426614174001",
      "ownerId": "123e4567-e89b-12d3-a456-426614174000",
      "serviceId": "789e0123-e89b-12d3-a456-426614174000",
      "startDateTime": "2025-12-10T10:00:00Z",
      "endDateTime": "2025-12-10T11:00:00Z",
      "status": 1,
      "notes": "First time visit",
      "createdAt": "2025-12-09T10:00:00Z",
      "updatedAt": "2025-12-09T10:00:00Z"
    }
  ]
}
```

#### Cancel Booking
```http
PUT /api/bookings/{id}/cancel
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Cancel a booking

**Remarks:**
- **CUSTOMERS ONLY:** Only customers can cancel bookings
- Can only cancel their own bookings
- Cannot cancel bookings that are already cancelled or completed

**Parameters:**
- `id` (GUID, required) - Booking ID to cancel

**Response:** Updated booking with status changed to Cancelled

## gRPC Integration with Availability Service

The Booking Service communicates with the Availability Service via gRPC to validate time slot availability before creating bookings.

### gRPC Service Definition

The gRPC proto file is located at `src/BookingService.Api/Protos/availability.proto`:

```protobuf
syntax = "proto3";
package availability;
import "google/protobuf/timestamp.proto";

service AvailabilityService {
  rpc CheckTimeSlotAvailability(TimeSlotRequest) returns (TimeSlotResponse);
}

message TimeSlotRequest {
  string tenant_id = 1;
  string service_id = 2;
  google.protobuf.Timestamp start_time = 3;
  google.protobuf.Timestamp end_time = 4;
}

message TimeSlotResponse {
  bool is_available = 1;
  repeated ConflictInfo conflicts = 2;
}

message ConflictInfo {
  ConflictType type = 1;
  google.protobuf.Timestamp overlap_start = 2;
  google.protobuf.Timestamp overlap_end = 3;
}

enum ConflictType {
  CONFLICT_TYPE_UNSPECIFIED = 0;
  CONFLICT_TYPE_TIME_BLOCK = 1;
  CONFLICT_TYPE_WORKING_HOURS = 2;
  CONFLICT_TYPE_BOOKING = 3;
  CONFLICT_TYPE_BUFFER_TIME = 4;
}
```

### gRPC Communication Flow

1. **Before Creating a Booking:**
   - Booking Service validates the request parameters
   - Validates the service exists, belongs to the tenant, and is active
   - Calculates the end time based on service duration
   - Makes a synchronous gRPC call to Availability Service

2. **gRPC Request:**
   ```csharp
   var grpcRequest = new Availability.TimeSlotRequest
   {
       TenantId = tenantId.ToString(),
       ServiceId = serviceId.ToString(),
       StartTime = Timestamp.FromDateTime(startDateTime.ToUniversalTime()),
       EndTime = Timestamp.FromDateTime(endDateTime.ToUniversalTime())
   };
   ```

3. **Response Handling:**
   - If `is_available = true`: Proceed with creating the booking
   - If `is_available = false`:
     - Extract conflict details from the `conflicts` list
     - Build a detailed error message showing all conflicts
     - Throw ConflictException with the detailed message
   - Handle gRPC errors (timeouts, service unavailable) appropriately

4. **Conflict Processing:**
   When conflicts are returned, the Booking Service formats them into a user-friendly message:
   ```csharp
   var conflictDescriptions = availabilityResponse.Conflicts
       .Select(c => $"{c.Type}: {c.OverlapStart:HH:mm} - {c.OverlapEnd:HH:mm}");

   var errorMessage = availabilityResponse.Conflicts.Count > 0
       ? $"The requested time slot is not available due to the following conflicts: {string.Join(", ", conflictDescriptions)}"
       : "The requested time slot is not available";
   ```

### Configuration

The gRPC client is configured via `AvailabilityServiceGrpcSettings` in `appsettings.json`:

```json
{
  "AvailabilityServiceGrpc": {
    "Url": "http://localhost:5003" // Availability Service gRPC endpoint
  }
}
```

### Error Handling

The gRPC client handles various error scenarios:
- **Deadline Exceeded (5s timeout):** Returns "Service temporarily unavailable"
- **Service Unavailable:** Returns "Service currently unavailable"
- **Other RPC Errors:** Returns generic error message
- **All errors are wrapped in `ServiceUnavailableException`**

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `DATABASE_CONNECTION_STRING` | Yes | PostgreSQL connection string |
| `JWT_SECRET_KEY` | Yes | JWT signing key (minimum 128 bits) |
| `ASPNETCORE_ENVIRONMENT` | No | Environment (Development/Production) |
| `AvailabilityServiceGrpc__Url` | No | Availability Service gRPC URL (overrides appsettings) |

## Health Checks

- `GET /health` - Complete health check including database
- `GET /health/live` - Basic service liveness check
- `GET /health/ready` - Readiness check for dependencies

## Booking Status Values

| Value | Status | Description |
|-------|--------|-------------|
| 0 | Pending | Booking created but not yet confirmed |
| 1 | Confirmed | Booking is confirmed and scheduled |
| 2 | Completed | Booking has been completed |
| 3 | Cancelled | Booking has been cancelled