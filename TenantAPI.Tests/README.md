# TenantAPI Test Suite

Contains unit and integration tests verifying multi-tenant isolation and endpoint contracts.

## Running Tests

Execute from repository root:
`ash
dotnet test
`
"@
        Commit = "docs: add test suite documentation"
        Title = "docs: document test suite execution"
        Body = "Provides instructions for running xUnit integration tests."
    },
    @{
        Branch = "docs/update-main-readme"
        File = "README.md"
        Append = True
        Content = @"

## Developer Documentation

- [Architecture Guide](docs/ARCHITECTURE.md)
- [API Reference](docs/API_REFERENCE.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Security Policy](docs/SECURITY.md)
