# Architecture Overview

TenantAPI is designed with strict multi-tenant isolation principles.

## Key Components

- **TenantMiddleware**: Extracts tenant headers (X-Tenant-ID) and resolves the active tenant scope.
- **AppDbContext**: Applies EF Core query filters to enforce tenant data isolation.
- **ScreeningService**: Encapsulates clinical and screening evaluation workflows.
