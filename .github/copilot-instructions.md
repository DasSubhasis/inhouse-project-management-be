# Copilot Instructions - Inhouse Project Management API

## Architecture Overview

This is an ASP.NET Core 8.0 Web API for an internal project management system at ZiCorp. The API uses a **stored procedure-centric architecture** with direct SQL Server access via Dapper for data operations. Authentication is session-based with OTP email verification (no JWT middleware despite config presence).

**Key Architectural Decisions:**
- **No EF Core**: All data access is through stored procedures using Dapper + `Microsoft.Data.SqlClient`
- **Authentication**: JWT config exists in `appsettings.json` but middleware is explicitly removed (see [Program.cs](ProjectsApi/Program.cs#L100) comment: "JWT AUTH REMOVED")
- **Session Management**: Uses 30-minute in-memory sessions with persisted data protection keys in `DataProtectionKeys/`
- **CORS**: Permissive "AllowAll" policy configured for frontend integration

## Project Structure

```
ProjectsApi/
├── Controllers/          # API endpoints (Authorization, Login, Menu, PreSales, User, UserRole)
├── Services/            # Business logic (OtpEmailService for email operations)
├── DataProtectionKeys/  # Persistent session keys (auto-created on startup)
├── Docs/               # Static file hosting for documents (auto-created)
└── Templates/          # Email HTML templates (e.g., OtpEmail.html)
```

## Critical Conventions

### 1. Stored Procedure Pattern
**All controllers** follow this pattern for database operations:

```csharp
using var conn = new SqlConnection(_connectionString);
var parameters = new DynamicParameters();
parameters.Add("@ParamName", value);

// For SELECT queries returning data:
var result = await conn.QueryAsync<ModelType>("SP_ProcedureName", parameters, commandType: CommandType.StoredProcedure);

// For INSERT/UPDATE returning status:
var result = await conn.QueryFirstAsync<ResponseType>("SP_Insert", parameters, commandType: CommandType.StoredProcedure);

// For multiple result sets (e.g., PreSalesController):
using var multi = await conn.QueryMultipleAsync("SP_GetData", parameters, commandType: CommandType.StoredProcedure);
var firstSet = await multi.ReadFirstAsync();
var secondSet = await multi.ReadAsync();
```

**Stored Procedure Naming:**
- Use `SP_` prefix (e.g., `SP_PreSales_Create`, `SP_PreSales_GetAll`)
- Legacy procedures may use `usp_` prefix (e.g., `usp_Authorization_Insert`, `usp_GetAllMenus`)
- Both patterns are valid in this codebase

**Important:** Use `QueryFirstAsync<T>` (not `ExecuteAsync`) for procedures returning status messages. See [AuthorizationController.cs](ProjectsApi/Controllers/AuthorizationController.cs#L88-L93) for reference.

### 2. Error Logging Standard
Every controller must implement and use this error logging method:

```csharp
private async Task LogErrorAsync(string controller, string action, string errorMessage, string errorSource = "Controller")
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ControllerName", controller);
        parameters.Add("@ActionName", action);
        parameters.Add("@ErrorMessage", errorMessage);
        parameters.Add("@ErrorSource", errorSource);
        parameters.Add("@CreatedAt", DateTime.Now);
        
        await conn.ExecuteAsync("usp_InsertErrorLog", parameters, commandType: CommandType.StoredProcedure);
    }
    catch { } // Silent failure to avoid cascading errors
}
```

Call it in catch blocks: `await LogErrorAsync("ActionName", ex.Message);`

### 3. Response Format Convention
Standardize all API responses with:

```csharp
// Success (200):
return Ok(new { statusCode = 200, message = "Operation successful", data = result });

// Bad Request (400):
return BadRequest(new { statusCode = 400, message = "Validation failed" });

// Conflict (409):
return Conflict(new { statusCode = 409, message = "Duplicate entry" });

// Server Error (500):
return StatusCode(500, new { statusCode = 500, message = "Operation failed", error = ex.Message });
```

### 4. JSON Serialization for List Parameters
When passing lists/arrays to stored procedures, serialize to JSON:

```csharp
// Example from PreSalesController:
param.Add("@AttachmentUrls",
    model.AttachmentUrls != null && model.AttachmentUrls.Any()
        ? JsonSerializer.Serialize(model.AttachmentUrls)
        : null
);
```

The stored procedure will receive a JSON string and parse it accordingly.

### 5. Hierarchical Data Structuring
For nested data (menus, categories), use LINQ to transform flat database results:

```csharp
// Example from MenuController - build parent-child hierarchy:
var structuredMenus = rawMenus
    .Where(m => m.MainMenuId == null)
    .OrderBy(m => m.Order)
    .Select(main => new MenuStructured
    {
        MenuId = main.MenuId,
        MenuName = main.MenuName,
        Submenu = rawMenus
            .Where(child => child.MainMenuId == main.MenuId)
            .OrderBy(child => child.Order)
            .Select(child => new MenuStructured { /* map child */ })
            .ToList()
    })
    .ToList();
```

Use `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]` on optional nested properties.

### 6. Dependency Injection Setup
Services are registered in [Program.cs](ProjectsApi/Program.cs#L17) as scoped:

```csharp
builder.Services.AddScoped<OtpEmailService>();
```

Inject in controllers:
```csharp
private readonly OtpEmailService _otpEmailService;
public LoginController(IConfiguration config, OtpEmailService otpEmailService) { ... }
```

## Development Workflows

### Running the API
```bash
# Development mode (auto-opens Swagger UI at http://localhost:5080/swagger):
dotnet run --project ProjectsApi

# Or use Visual Studio launch profiles (see launchSettings.json)
```

### Database Connection
- Connection string: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- Production DB: `server=165.99.212.216,1434;Initial Catalog=DB_Projects;...`
- All stored procedures must exist in the `DB_Projects` database

### Environment-Specific Configuration
- `appsettings.json`: Base configuration for all environments
- `appsettings.Development.json`: Development overrides (currently only logging settings)
- Override pattern follows ASP.NET Core conventions - Development settings merge with base
- Add environment-specific overrides for connection strings, email settings, or CORS as needed

### Email Configuration
OTP emails use Gmail SMTP ([OtpEmailService.cs](ProjectsApi/Services/OtpEmailService.cs)):
- SMTP settings in `appsettings.json` → `EmailSettings`
- Email templates: Currently only OTP email is templated
  - Template location: `Templates/OtpEmail.html` (create this folder manually if needed)
  - If template file missing, falls back to inline HTML in `OtpEmailService.GetFallbackOtpHtml()`
- Manager email for notifications: `pr@zicorp.in`
- To add new email templates: Follow the same pattern as `SendOtpEmailAsync()` method

### Static Files & Documentation
The API serves static files from the `Docs/` folder:
- Accessible at `/Docs` endpoint with directory browsing enabled
- Created automatically on startup if missing

## Integration Points

### Frontend URLs (CORS whitelist)
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",      // Dev (React/Next.js default)
    "http://localhost:5173",      // Dev (Vite default)
    "https://connect.zicorp.co.in" // Production frontend
  ]
}
```

### Authentication Flow
1. User enters email → `POST /api/login/request-otp`
2. OTP generated, stored in DB (via `usp_RequestOtp`), emailed via `OtpEmailService`
3. User submits OTP → `POST /api/login/verify-otp`
4. On success, session established (30-min timeout)

**Note:** No JWT tokens in use despite configuration presence.

## Common Patterns

### Model Classes
Define inline within controllers as nested classes:
```csharp
public class UserInsertModel
{
    public string? UserName { get; set; }
    public string? EmailId { get; set; }
    public Guid? RoleId { get; set; }
}
```

### Guid Handling
- Primary keys are `Guid` types (e.g., `UserId`, `RoleId`, `MenuId`)
- Pass as parameters: `parameters.Add("@UserId", userId);`

### DateOnly Swagger Mapping
Custom mapping configured in [Program.cs](ProjectsApi/Program.cs#L68-L73):
```csharp
opt.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date", Example = new OpenApiString("2025-12-17") });
```

## Important Notes

- **No Authentication Middleware**: Despite JWT config, `app.UseAuthentication()` is commented out
- **AllowAll CORS**: Development convenience; consider tightening for production
- **In-Memory Sessions**: Will reset on app restart unless using distributed cache
- **Namespace**: All code uses `SWCAPI` namespace (likely legacy from "ZiCorp Web Call API")
- **Swagger Pin**: Documented in config (`SwaggerPin: "SWC2025"`) but not enforced in code

## Testing
- **Manual Testing**: Access Swagger UI at `/swagger` after starting the app. All endpoints documented automatically via Swashbuckle.
- **Automated Testing**: No unit/integration test projects in this repository. Testing is performed externally.
- Use Swagger's "Try it out" feature to test endpoints interactively during development.
