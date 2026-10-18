# Design decisions

Each entry names the decision, the alternatives we rejected and the main trade-off.

## MVC with Razor views instead of a React front end

- **Alternatives:** ASP.NET Core Web API with a separate React application.
- **Why:** The assignment asks for a controller, view model and view, and one project keeps login, roles and two-factor simple. JavaScript is used only where the browser must do the work: the map.
- **Trade-off:** Less client-side interactivity. We accept this because the map is the only highly interactive page.

## Entity Framework Core instead of Dapper

- **Alternatives:** Dapper, a lightweight micro-ORM that maps SQL query results to C# objects.
- **Why:** The application has several related entities, including needs, resources, users and matches. EF Core provides LINQ queries, relationship mapping, change tracking, migrations and integration with the existing unit-of-work pattern. These features reduce the amount of manual SQL and persistence code required for the application.
- **Trade-off:** EF Core introduces abstraction and change-tracking overhead, and complex queries may require additional optimization. Dapper offers more direct SQL control and can be a better fit for specialized, read-heavy queries, but would require more manual query and persistence management.

## Cookie authentication with our own `AuthService` instead of ASP.NET Core Identity

- **Alternatives:** The full Identity framework with its user store.
- **Why:** The `User` model already used integer keys that other tables reference. Identity would have meant reworking those keys. We still use the framework's `PasswordHasher` and a TOTP library, so we do not write our own cryptography.
- **Trade-off:** We maintain the login flow ourselves and have no lockout, recovery codes or secret encryption. See [Authentication](./features/authentication.md).

## Map data loaded by visible bounds, filtered on the server

- **Why:** Loading everything on every page view does not scale, and sending other providers' resources to the browser would leak data even if the JavaScript hid them. The server filters by the visible area, the user's role and a cap of 500 markers per kind, and reports when the cap truncated the result.

## S3-compatible object storage, storing only the object key

- **Alternatives:** Files on the container disk, or full URLs in the database.
- **Why:** Container disks need volumes and are not shared between teammates. The `IFileStorage` interface hides the provider, so moving to AWS S3, R2 or MinIO is a configuration change. Storing the key and building the URL when rendering keeps rows valid if the bucket address changes.
- **Trade-off:** The bucket is public, so anyone with an image link can open it. Keys contain random GUIDs and only non-sensitive photos are accepted.

## Repositories and a unit of work in front of EF Core

- **Alternatives:** Call `DbContext` directly from services.
- **Why:** Services can be unit tested with mocked repositories, and one `CommitAsync` per operation makes partial writes impossible.
- **Trade-off:** More files than strictly necessary, since EF Core already behaves like a repository.

## Matches are created by a person, suggestions are computed

- **Alternatives:** Store automatically generated matches.
- **Why:** A match locks a resource and changes the status of a need, so it must be a deliberate, traceable act. That is why `matched_by_user_id` is required. Suggestions (same category, within 3 km, available, and the availability window fits the deadline) are a live query that is always up to date and needs no table.
- **Trade-off:** Suggestions cost a query each time they are shown, which is cheap at our data size.

## Three separate status fields

- A **need** (`New`, `UnderReview`, `Assigned`, `Resolved`), a **resource** (`Available`, `Busy`) and a **match** (`UnderReview`, `Assigned`, `Resolved`, `Rejected`) each have their own status. A resource can be wanted by several needs over time, and each pairing has its own progress, so one shared status would conflate three different questions.

## Fixed vocabularies instead of free text

- Statuses, priorities and roles are C# enums stored as strings so the database stays readable. Categories are seeded rows. Free text would let "Kristiansand" and "kristiansand kommune" split the same region in two and break the print-by-region requirement.

## Region derived from the map position

- **Alternatives:** A free-text field, or a dropdown.
- **Why:** The user already clicks a position, so asking for the region again is double entry that can disagree with the position. The server derives the region from the coordinates, and the value posted by the browser is never trusted.
- **Trade-off:** This depends on an external geocoding service, so a lookup failure must not block registration.

## A separate Migrator project

- **Alternatives:** Run migrations at web start-up.
- **Why:** Migrations run once, in one place, before any web instance starts. Reference data (categories) is seeded in every environment, and development data (demo users, sample needs, photos) only in Development, so production never receives accounts with known passwords.

## .NET Aspire for local orchestration

- **Alternatives:** Docker Compose, which the assignment also allows.
- **Why:** Aspire expresses startup order (`WaitForCompletion`) directly, injects connection strings and gives a dashboard with logs. Docker Compose is used for deployment, see [Deployment](./deployment.md).

## Persistent development database, never-edited migrations

- Data survives restarts, so teammates keep their test data. The rule that follows is that every schema change is a new migration.

## Not built, on purpose

- **Admin role and OAuth login:** not required, and categories are seeded. They are candidates for future work.
