# SmartShip Database Map

## Service to Database to Tables

- `SmartShip.AdminService`
  - Database: `SmartShipAdminDb`
  - Tables:
    - `Hubs`
    - `ServiceLocations`
    - `ExceptionRecords`

- `SmartShip.DocumentService`
  - Database: `SmartShipDocumentDb`
  - Tables:
    - `Documents`
    - `DeliveryProofs`

- `SmartShip.IdentityService`
  - Database: `SmartShipIdentityDb`
  - Tables:
    - `Roles`
    - `Users`
    - `RefreshTokens`
    - `PasswordResetTokens`

- `SmartShip.ShipmentService`
  - Database: `SmartShipShipmentDb`
  - Tables:
    - `Addresses`
    - `Shipments`
    - `Packages`
    - `PickupSchedules`

- `SmartShip.TrackingService`
  - Database: `SmartShipTrackingDb`
  - Tables:
    - `TrackingLogs`

- `SmartShip.Gateway`
  - Database: Not configured
  - Tables: Not applicable

---

## Table Structures

### `SmartShip.AdminService` (`SmartShipAdminDb`)

#### `Hubs`
- `HubId` (`uniqueidentifier`, PK)
- `Name` (`nvarchar(200)`, required)
- `Address` (`nvarchar(500)`, required)
- `ManagerName` (`nvarchar(200)`, required)
- `ContactNumber` (`nvarchar(30)`, required)
- `IsActive` (`bit`, required)
- `CreatedAt` (`datetime2`, required)

#### `ServiceLocations`
- `LocationId` (`uniqueidentifier`, PK)
- `HubId` (`uniqueidentifier`, FK -> `Hubs.HubId`, required)
- `Name` (`nvarchar(200)`, required)
- `ZipCode` (`nvarchar(20)`, required)
- `IsActive` (`bit`, required)

#### `ExceptionRecords`
- `ExceptionId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, required)
- `ExceptionType` (`nvarchar(100)`, required)
- `Description` (`nvarchar(1000)`, required)
- `Status` (`nvarchar(50)`, required)
- `CreatedAt` (`datetime2`, required)
- `ResolvedAt` (`datetime2`, nullable)

### `SmartShip.DocumentService` (`SmartShipDocumentDb`)

#### `Documents`
- `DocumentId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, required)
- `CustomerId` (`uniqueidentifier`, required)
- `DocumentType` (`nvarchar(100)`, required)
- `FileName` (`nvarchar(255)`, required)
- `ContentType` (`nvarchar(150)`, required)
- `FilePath` (`nvarchar(1000)`, required)
- `UploadedAt` (`datetime2`, required)

#### `DeliveryProofs`
- `ProofId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, required)
- `SignerName` (`nvarchar(200)`, required)
- `FilePath` (`nvarchar(1000)`, required)
- `Notes` (`nvarchar(1000)`, nullable)
- `Timestamp` (`datetime2`, required)

### `SmartShip.IdentityService` (`SmartShipIdentityDb`)

#### `Roles`
- `RoleId` (`uniqueidentifier`, PK)
- `RoleName` (`nvarchar(100)`, required, unique)

#### `Users`
- `UserId` (`uniqueidentifier`, PK)
- `RoleId` (`uniqueidentifier`, FK -> `Roles.RoleId`, required)
- `Name` (`nvarchar(200)`, required)
- `Email` (`nvarchar(320)`, required, unique)
- `Phone` (`nvarchar(20)`, nullable)
- `PasswordHash` (`nvarchar(max)`, required)
- `CreatedAt` (`datetime2`, required)
- `UpdatedAt` (`datetime2`, required)

#### `RefreshTokens`
- `RefreshTokenId` (`uniqueidentifier`, PK)
- `UserId` (`uniqueidentifier`, FK -> `Users.UserId`, required)
- `TokenHash` (`nvarchar(max)`, required)
- `CreatedAt` (`datetime2`, required)
- `ExpiresAt` (`datetime2`, required)
- `IsRevoked` (`bit`, required)

#### `PasswordResetTokens`
- `PasswordResetTokenId` (`uniqueidentifier`, PK)
- `UserId` (`uniqueidentifier`, FK -> `Users.UserId`, required)
- `TokenHash` (`nvarchar(max)`, required)
- `CreatedAt` (`datetime2`, required)
- `ExpiresAt` (`datetime2`, required)
- `IsUsed` (`bit`, required)

### `SmartShip.ShipmentService` (`SmartShipShipmentDb`)

#### `Addresses`
- `AddressId` (`uniqueidentifier`, PK)
- `Street` (`nvarchar(300)`, required)
- `City` (`nvarchar(100)`, required)
- `State` (`nvarchar(100)`, required)
- `PostalCode` (`nvarchar(20)`, required)
- `Country` (`nvarchar(100)`, required)

#### `Shipments`
- `ShipmentId` (`uniqueidentifier`, PK)
- `CustomerId` (`uniqueidentifier`, required)
- `SenderAddressId` (`uniqueidentifier`, FK -> `Addresses.AddressId`, required)
- `ReceiverAddressId` (`uniqueidentifier`, FK -> `Addresses.AddressId`, required)
- `TrackingNumber` (`nvarchar(100)`, required, unique)
- `Status` (`nvarchar(50)`, required)
- `TotalWeight` (`decimal(18,2)`, required)
- `CreatedAt` (`datetime2`, required)
- `UpdatedAt` (`datetime2`, required)

#### `Packages`
- `PackageId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, FK -> `Shipments.ShipmentId`, required)
- `Weight` (`decimal(18,2)`, required)
- `Length` (`decimal(18,2)`, required)
- `Width` (`decimal(18,2)`, required)
- `Height` (`decimal(18,2)`, required)
- `Description` (`nvarchar(500)`, nullable)

#### `PickupSchedules`
- `PickupScheduleId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, FK -> `Shipments.ShipmentId`, required)
- `PickupDate` (`datetime2`, required)
- `Notes` (`nvarchar(1000)`, nullable)

### `SmartShip.TrackingService` (`SmartShipTrackingDb`)

#### `TrackingLogs`
- `TrackingLogId` (`uniqueidentifier`, PK)
- `ShipmentId` (`uniqueidentifier`, required)
- `TrackingNumber` (`nvarchar(100)`, required)
- `Status` (`nvarchar(50)`, required)
- `Location` (`nvarchar(200)`, required)
- `Description` (`nvarchar(1000)`, required)
- `Timestamp` (`datetime2`, required)
