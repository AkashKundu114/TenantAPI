# TenantAPI — Multi-Tenant Ophthalmic Screening REST API

A minimal multi-tenant REST API built with **ASP.NET Core 8**, **Entity Framework Core**, and **JWT authentication**. Demonstrates real-world patterns used in enterprise SaaS platforms — the same stack used at DeltaX.

## What This Demonstrates

| Skill                          | How It's Used                                                  |
| ------------------------------ | -------------------------------------------------------------- |
| **ASP.NET Core Web API**       | Controllers, middleware pipeline, `ProblemDetails` responses   |
| **Entity Framework Core**      | Code-first models, `DbContext`, SQLite (dev) / SQL Server (prod) |
| **Multi-Tenancy**              | Custom middleware extracts `tenant_id` from JWT claims          |
| **Tenant Isolation**           | Every LINQ query filtered by `TenantId` — no cross-tenant leaks |
| **JWT Authentication**         | Bearer token validation with symmetric key                     |
| **Role-Based Authorization**   | `[Authorize(Roles = "Admin")]` on admin-only endpoints         |
| **Dependency Injection**       | `IScreeningService` → `ScreeningService` registered as scoped  |
| **LINQ**                       | `Where`, `OrderByDescending`, `GroupBy`, `Select`, `Count`, `Average` |
| **Async/Await**                | All database operations are fully asynchronous                 |
| **Nullable Reference Types**   | Enabled project-wide (`<Nullable>enable</Nullable>`)           |
| **Swagger / OpenAPI**          | Auto-generated API docs at `/swagger`                          |
| **Integration Testing (xUnit)**| 12 tests using `WebApplicationFactory` with InMemory EF Core   |

## Connection to OphthalmoAI

This API models the **same domain** as my [OphthalmoAI](https://github.com/AkashKundu114/OphthalmoAI) research project — automated ophthalmic disease screening — but demonstrates it through a **different technology stack**:

| Aspect         | OphthalmoAI (Python)        | TenantAPI (C#)                    |
| -------------- | --------------------------- | --------------------------------- |
| **Purpose**    | ML model training & inference | Multi-tenant API serving results |
| **Language**   | Python                      | C# / .NET 8                      |
| **Framework**  | PyTorch, FastAPI             | ASP.NET Core, EF Core            |
| **Database**   | SQLite / PostgreSQL          | SQL Server / SQLite               |
| **Focus**      | Deep learning pipeline       | Enterprise SaaS architecture      |

The `ScreeningResult` model in this API mirrors what OphthalmoAI's ML pipeline produces — diagnoses like *Diabetic Retinopathy*, *Glaucoma*, and *Age-Related Macular Degeneration* with confidence scores.

## API Endpoints

| Method | Route                         | Auth         | Description                        |
| ------ | ----------------------------- | ------------ | ---------------------------------- |
| GET    | `/api/tenants`                | Admin only   | List all tenants                   |
| GET    | `/api/screenings`             | Authenticated | List screenings for current tenant |
| POST   | `/api/screenings`             | Authenticated | Create a new screening result      |
| GET    | `/api/screenings/{id}`        | Authenticated | Get specific screening by ID       |
| GET    | `/api/screenings/statistics`  | Authenticated | Aggregated stats (GroupBy + Avg)   |

## How to Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Run the API
```bash
cd TenantAPI
dotnet run
```
The API starts at `https://localhost:5001` (or the port shown in console).  
Swagger UI: `https://localhost:5001/swagger`

### Run Tests
```bash
dotnet test
```

All 12 tests should pass:
```
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```

### Generate a Test JWT (for manual testing)
Use any JWT tool (e.g., [jwt.io](https://jwt.io)) with:
```json
{
  "sub": "user1",
  "tenant_id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "role": "Admin",
  "iss": "TenantAPI",
  "aud": "TenantAPI"
}
```
**Signing key:** `DevSecret_ChangeInProduction_MinLength32Chars!` (HS256)

## Architecture

```
TenantAPI/
├── Controllers/
│   ├── TenantsController.cs        # Admin-only tenant listing
│   └── ScreeningsController.cs     # CRUD + statistics (tenant-scoped)
├── Data/
│   └── AppDbContext.cs             # EF Core context with relationships
├── Middleware/
│   └── TenantMiddleware.cs         # Extracts tenant_id from JWT → HttpContext
├── Models/
│   ├── Tenant.cs                   # Tenant entity
│   ├── ScreeningResult.cs          # Screening entity (FK to Tenant)
│   └── Dtos.cs                     # Request/response records
├── Services/
│   ├── IScreeningService.cs        # Service interface (DI contract)
│   └── ScreeningService.cs         # LINQ queries, CRUD logic
└── Program.cs                      # DI, auth, middleware, pipeline
```

### Key Design Decisions

1. **Tenant isolation via LINQ filtering** — every query includes `.Where(s => s.TenantId == tenantId)`, ensuring no data crosses tenant boundaries. This is the simplest and most auditable approach for multi-tenancy.

2. **SQLite for dev, SQL Server for prod** — `Program.cs` selects the database provider based on environment, making local development frictionless while supporting production SQL Server.

3. **Service layer (not repository pattern)** — keeps the codebase small while still demonstrating DI. The service encapsulates all LINQ logic behind an interface.

4. **ProblemDetails for errors** — uses the standard RFC 7807 format (`Problem()`) instead of ad-hoc error responses.

5. **Records for DTOs** — immutable, concise, and idiomatic C# 12.

## License

MIT
