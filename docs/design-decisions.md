# Design decisions

Each entry names the decision, the alternatives we rejected and the main trade-off.

## Coordinates as two numbers instead of NetTopologySuite geometry

- **Alternatives:** NetTopologySuite with a MariaDB `POINT` column (via `Pomelo.EntityFrameworkCore.MySql.NetTopologySuite`, which also supports MariaDB).
- **Why:** We only store points. A region is a text field, not a polygon, so we need no geometry operations such as contains, intersects or buffer. Distance must be in kilometres on a sphere. NetTopologySuite calculates on a plane, so with latitude and longitude it returns degrees. `Latitude` and `Longitude` are stored as two `DOUBLE` columns, and `GeoMath` (`Infrastructure/Geo/GeoMath.cs`) computes the Haversine distance and a bounding box. It is small, pure and easy to unit test, and the entities do not depend on a geometry library.
- **Trade-off:** We can't answer polygon questions such as "which HV district contains this point" or import GeoJSON shapes. If regions become real borders, NetTopologySuite becomes the right tool.

## B-tree indexes instead of a spatial (R-tree) index

- **Alternatives:** A `SPATIAL` index on a `POINT` column.
- **Why:** MariaDB treats geometry as flat (Cartesian) coordinates. Its only spherical distance function, `ST_DISTANCE_SPHERE`, can't use a spatial index directly. The textbook query therefore still filters with a box (`MBRContains`) first and then computes the exact distance. Our queries do the same: a bounding-box range on latitude and longitude in SQL, then Haversine on the remaining rows. Ordinary B-tree indexes on the equality filters (`CategoryId`, `UserId`) narrow the rows before the coordinate check. A spatial index would also require a `NOT NULL` `POINT` column, and so would bring in NetTopologySuite and a migration of every coordinate.
- **Trade-off:** A B-tree can only seek on one range column, so latitude is used to seek while longitude is only checked. A spatial index would handle both dimensions at once.

## Live bounds API instead of FlatGeobuf files

- **Alternatives:** Export the markers to a [FlatGeobuf](https://flatgeobuf.org/) file in object storage. The file has a built-in spatial index, so the browser can fetch only the features in view with HTTP range requests.
- **Why:** FlatGeobuf is made for large, static datasets. Our data changes all the time: resources are created and edited, statuses go from `Available` to `Busy`, and needs are resolved. Each change would mean regenerating the file. A static file is also the same for everyone, while the map depends on the user: providers see their own resources, others see only available ones. A public file would leak contact points that the API filters out. It also can't apply per-request rules such as "critical needs first" or the 500-marker cap. Our map already loads only the visible area (see [Map data loaded by visible bounds](#map-data-loaded-by-visible-bounds-filtered-on-the-server)).
- **Trade-off:** Every pan or zoom costs a server request and a database query. The requests are debounced. FlatGeobuf could still fit later as a static background layer, such as HV district borders under the live markers.

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
