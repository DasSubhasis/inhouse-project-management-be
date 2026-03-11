# License Management API Documentation

## Overview
This API provides CRUD operations for managing licenses with validation capabilities based on tally serial numbers and project IDs. All operations use **inline SQL queries** with Dapper for database access.

## Database Setup
Before using the API, execute the SQL script to create the database table:
- **Location**: `Docs/LicenseTableAndProcedures.sql`
- **Objects Created**:
  - Table: `tbl_License`
  - Index: `IX_tbl_License_TallySerial_ProjectId`
  
**Note**: No stored procedures are required. All CRUD operations are performed using inline SQL queries in the controller.

## API Endpoints

### Base URL
```
/api/License
```

### 1. Insert License
**Endpoint**: `POST /api/License/insert`

**Request Body**:
```json
{
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectName": "Sample Project",
  "tallySerial": "TSN12345",
  "licenseNo": "LIC-2026-001",
  "initiationDate": "2026-01-01",
  "expiryDate": "2027-01-01",
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "License inserted successfully"
}
```

---

### 2. Get All Licenses
**Endpoint**: `GET /api/License/getall`

**Response**:
```json
[
  {
    "licenseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "projectName": "Sample Project",
    "tallySerial": "TSN12345",
    "licenseNo": "LIC-2026-001",
    "initiationDate": "2026-01-01",
    "expiryDate": "2027-01-01",
    "createdDate": "2026-03-09T10:30:00",
    "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "modifiedDate": null,
    "modifiedBy": null,
    "deleteDate": null,
    "deletedBy": null
  }
]
```

---

### 3. Get License By ID
**Endpoint**: `GET /api/License/getbyid/{id}`

**Parameters**:
- `id` (GUID): License ID

**Response**:
```json
{
  "licenseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectName": "Sample Project",
  "tallySerial": "TSN12345",
  "licenseNo": "LIC-2026-001",
  "initiationDate": "2026-01-01",
  "expiryDate": "2027-01-01",
  "createdDate": "2026-03-09T10:30:00",
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "modifiedDate": null,
  "modifiedBy": null,
  "deleteDate": null,
  "deletedBy": null
}
```

---

### 4. Update License
**Endpoint**: `POST /api/License/update`

**Request Body**:
```json
{
  "licenseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "projectName": "Updated Project",
  "tallySerial": "TSN12345",
  "licenseNo": "LIC-2026-001-UPDATED",
  "initiationDate": "2026-01-01",
  "expiryDate": "2027-06-01",
  "modifiedBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "License updated successfully"
}
```

---

### 5. Delete License (Soft Delete)
**Endpoint**: `POST /api/License/delete/{id}`

**Parameters**:
- `id` (GUID): License ID

**Request Body** (GUID):
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Response**:
```json
{
  "statusCode": 200,
  "message": "License deleted successfully"
}
```

---

### 6. Validate License (Special Endpoint)
**Endpoint**: `GET /api/License/validate`

**Query Parameters**:
- `tallySerial` (string): Tally Serial Number
- `licenseNo` (string): License Number

**Example**: `/api/License/validate?tallySerial=TSN12345&licenseNo=LIC-2026-001`

**Response (Valid)**:
```json
{
  "status": "Y",
  "message": "License is valid"
}
```

**Response (Expired)**:
```json
{
  "status": "N",
  "message": "License has expired"
}
```

**Response (Not Found)**:
```json
{
  "status": "N",
  "message": "License not found"
}
```

**Validation Logic**:
- Checks if a license exists with the given `tallySerial` and `licenseNo`
- Compares `expiryDate` with today's date
- Returns `Y` if license exists and is not expired
- Returns `N` if license is expired or not found

---

## Error Handling
All endpoints return appropriate HTTP status codes and error messages:

**Success**: `200 OK`
```json
{
  "statusCode": 200,
  "message": "Success message"
}
```

**Not Found**: `404 Not Found`
```json
{
  "statusCode": 404,
  "message": "License not found"
}
```

**Server Error**: `500 Internal Server Error`
```json
{
  "statusCode": 500,
  "message": "Operation failed",
  "error": "Detailed error message"
}
```

---

## Database Table Structure

### tbl_License
| Column | Type | Description |
|--------|------|-------------|
| license_id | UNIQUEIDENTIFIER | Primary Key |
| project_id | UNIQUEIDENTIFIER | Project identifier |
| project_name | NVARCHAR(255) | Project name |
| tally_serial | NVARCHAR(100) | Tally serial number |
| license_no | NVARCHAR(100) | License number |
| initiation_date | DATE | License start date (date only) |
| expiry_date | DATE | License expiry date (date only) |
| created_date | DATETIME | Record creation timestamp |
| created_by | UNIQUEIDENTIFIER | User who created the record |
| modified_date | DATETIME | Last modification timestamp |
| modified_by | UNIQUEIDENTIFIER | User who last modified |
| delete_date | DATETIME | Soft delete timestamp |
| deleted_by | UNIQUEIDENTIFIER | User who deleted the record |

---

## Implementation Details

### Technology Stack
- **Framework**: ASP.NET Core Web API
- **ORM**: Dapper (Micro-ORM)
- **Database**: SQL Server
- **Query Approach**: Inline SQL queries (no stored procedures)

### Key Features
- Lightweight and fast data access using Dapper
- Parameterized queries for SQL injection prevention
- Soft delete implementation (records marked as deleted, not physically removed)
- Automatic error logging using `usp_InsertErrorLog` stored procedure
- License validation with expiry date checking

---

## Notes
- The API uses **soft delete** - records are not physically removed from the database
- **Date fields** (`initiationDate`, `expiryDate`) accept date-only format: `"YYYY-MM-DD"` (e.g., `"2026-03-09"`)
- **DateTime fields** (`createdDate`, `modifiedDate`, `deleteDate`) include both date and time
- License validation compares expiry date with the current server date
- Error logs are automatically created using `usp_InsertErrorLog` stored procedure
