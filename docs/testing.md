# Testing

The project has two automated test projects with 201 tests in total, and all of them pass.

| Project                        | Tests | What it tests                                                    | Needs                                |
| ------------------------------ | ----- | ---------------------------------------------------------------- | ------------------------------------ |
| `Heimevernet.UnitTests`        | 87    | Services, mappers and the exporter, in isolation                 | Nothing                              |
| `Heimevernet.IntegrationTests` | 114   | Full HTTP requests through controllers, services and a real database | Docker running and the storage secrets |

## Unit tests

Unit tests check business rules in isolation. They use **xUnit** and **Moq**: repositories, the unit of work and other services are mocked, while the real mappers are used inside service tests. A mapping change is therefore tested both directly and through the services that use it.

| Test class                               | Checks                                                                                                                                                                                                                                     |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `NeedServiceTests`, `ResourceServiceTests` | Create saves and commits, and a repository failure does not commit. Get returns mapped view models, including empty and not-found cases. Update and delete check the owner, reject protected-field changes while a match is pending, return a conflict on a concurrency conflict, and never change the owner, status or creation time. |
| `AuthServiceTests`                       | Registration hashes the password, trims input and rejects an existing email. Login handles an unknown email, a wrong password, and accounts with and without two-factor. Two-factor setup returns a QR code and only enables 2FA after a valid code. Sign-in issues the expected claims. |
| `ResourceExporterTests`                  | CSV starts with a UTF-8 BOM, follows the header order, formats dates, quotes special characters and neutralises formula prefixes (`=`, `+`, `-`, `@`, tab and carriage return) to prevent CSV injection. JSON exports only the agreed fields, uses ISO dates and keeps Norwegian letters readable. |
| `NeedMapperTests`, `ResourceMapperTests` | Mapping from entity to view model and back, trimming, nullable fields and empty collections.                                                                                                                                                       |

## Integration tests

Integration tests send real HTTP requests to the application and check the response and the resulting database state. They show that controllers, authorisation, validation, services, EF Core and the database work together.

### How they work

- **`HeimevernetWebApplicationFactory`** starts the real application in memory with `WebApplicationFactory` in the `Testing` environment.
- **Testcontainers** starts a fresh **MariaDB 11** container, the same engine as in production. The factory applies the EF Core migrations and seeds the reference data (categories) before the first test.
- Each test class gets its own factory and database through `IClassFixture`. Inside a class, every test creates its own users with unique emails through **`TestDataFactory`**, so tests do not depend on each other.
- **`AuthenticatedClientHelper`** logs a test user in and returns a client with the login cookie.
- **`AntiforgeryHelper`** reads the anti-forgery token from a form and posts it back, just like a browser. Form posts therefore go through the same CSRF protection as in production.
- File storage uses the separate test bucket `test_bucket_is202`, so tests never touch development or production files.

### What they cover

| Area          | Test classes                                                                                                     | Checks                                                                                                                                                                                                                                                                                           |
| ------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Account       | `LoginTests`, `LogoutTests`, `RegistrationTests`, `TwoFactorAuthenticationTests`, `TwoFactorSetupTests`, `AccountPageTests` | Valid and invalid login with a generic error message, logout only through POST, registration without duplicates, the full two-factor login and setup flow, and protected pages redirecting anonymous users to login.                                                                              |
| Needs         | `CreateNeedTests`, `EditNeedTests`, `DeleteNeedTests`                                                            | Creating with validation (title, category, quantity, latitude). Only the owner may edit or delete. Resource providers and anonymous users are refused. A need with a pending or assigned match cannot be deleted.                                                                                  |
| Resources     | `CreateResourceTests`, `EditResourceTests`, `DeleteResourceTests`                                                | Creating with validation, including `AvailableTo` before `AvailableFrom`. Only the owner may edit or delete, and a resource with a match cannot be deleted.                                                                                                                                         |
| Export        | `ExportResourceTests`                                                                                            | CSV and JSON export of available resources, with and without a region filter. Only the allowed role can export, and the print-by-region page keeps the selected region in its export links.                                                                                                      |
| Matching      | `MatchProposalTests`, `MatchAcceptanceTests`, `MatchRejectionTests`                                              | Proposing (no duplicates, valid quantity, only the need owner, not to busy resources). Accepting and rejecting (only the resource owner, only proposals under review, quantity limits on both sides). When a need or resource becomes fully allocated, other pending proposals are cancelled. After a rejection, the need's status falls back correctly. |
| Map           | `MapControllerTests`                                                                                             | Public actors see needs and available resources, while providers see needs and their own resources. Resolved needs and markers outside the bounds are excluded, invalid bounds return `400`, and the 500-marker cap sets `Truncated`.                                                              |

## What is not covered yet

| Area                                      | Status                                                                                                                    |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `MatchService` unit tests                 | `MatchServiceTests` is an empty placeholder. Matching is covered end to end by 22 integration tests.                      |
| `GeoMath` (distance and bounding box)     | No tests. It is pure logic and a good candidate for unit tests.                                                           |
| Search                                    | `SearchController` and `SearchRepository` have no tests.                                                                  |
| Attachments                               | Upload, validation and storage are only mocked inside other tests, never tested directly.                                 |
| Audit logging                             | No tests check that actions write audit entries.                                                                          |
| Nearby resources and suggestions on a need | No tests.                                                                                                                 |
| Razor views and the map JavaScript        | No automated browser tests. These are checked by hand.                                                                    |

## Run the tests

Run everything from the repository folder:

```bash
dotnet test Heimevernet.slnx
```

Or run one project at a time:

```bash
dotnet test Heimevernet.UnitTests          # no requirements, under a second
dotnet test Heimevernet.IntegrationTests   # needs Docker and the storage secrets, about 20 seconds
```

The integration tests need **Docker Desktop running**, so Testcontainers can start MariaDB, and the **storage secrets** from [Getting started](./getting-started.md#_2-set-the-storage-secrets), because the application validates its storage settings at start-up. The first run also downloads the MariaDB image.

## Continuous integration

GitHub Actions runs the CI workflow on every push and pull request targeting `main`. It has two jobs that run in parallel on `ubuntu-latest`.

**`ci`: build and all tests**

1. Checks out the repository and installs the .NET 10 SDK.
2. Restores and builds `Heimevernet.slnx` in Release configuration.
3. Runs **both** test projects with `dotnet test Heimevernet.slnx`. The integration tests work in CI because the GitHub runners have Docker for Testcontainers, and the storage settings come from repository secrets as environment variables (`Storage__Endpoint` and so on, where `__` stands for `:` in .NET configuration).
4. Publishes the TRX results of both projects as a GitHub Actions check, even if an earlier step fails.

**`docs`: documentation build**

Installs Node.js 22, runs `npm ci` and `npm run docs:build`, so a broken link or invalid page fails the pull request before it reaches GitHub Pages.

The storage secrets (`STORAGE_ENDPOINT`, `STORAGE_REGION`, `STORAGE_ACCESS_KEY_ID`, `STORAGE_SECRET_ACCESS_KEY`, `STORAGE_BUCKET`, `STORAGE_PUBLIC_BASE_URL`) are set under the repository's **Settings → Secrets and variables → Actions**. GitHub does not pass secrets to pull requests from forks, so the integration tests only run for branches in this repository.

The workflow is defined in `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main

jobs:
  ci:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore Heimevernet.slnx

      - name: Build
        run: dotnet build Heimevernet.slnx --no-restore --configuration Release

      - name: Test
        env:
          Storage__Endpoint: ${{ secrets.STORAGE_ENDPOINT }}
          Storage__Region: ${{ secrets.STORAGE_REGION }}
          Storage__AccessKeyId: ${{ secrets.STORAGE_ACCESS_KEY_ID }}
          Storage__SecretAccessKey: ${{ secrets.STORAGE_SECRET_ACCESS_KEY }}
          Storage__Bucket: ${{ secrets.STORAGE_BUCKET }}
          Storage__PublicBaseUrl: ${{ secrets.STORAGE_PUBLIC_BASE_URL }}
        run: >
          dotnet test Heimevernet.slnx
          --no-build
          --configuration Release
          --logger "trx;LogFileName=test-results.trx"

      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results (Unit & Integration)
          path: "**/test-results.trx"
          reporter: dotnet-trx

  docs:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: npm

      - name: Install dependencies
        run: npm ci

      - name: Build documentation
        run: npm run docs:build
```

## Latest test result

![Latest tests](./assets/tests.png)
