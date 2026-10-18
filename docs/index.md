# Heimevernet

Heimevernet is a web portal for crisis response. **Public actors** (municipalities, police, fire, health, civil defence and the armed forces) register _needs_ for assistance. **Resource providers** (private persons, businesses, farmers, contractors, volunteer organisations and drone operators) register _resources_ they can offer, optionally with photos. Needs and resources are shown together on a map, and the system suggests resources that fit a need.

The project is built for IS-202 with ASP.NET Core MVC (.NET 10), MariaDB, Leaflet, cookie authentication with two-factor authentication, .NET Aspire for local orchestration and GitHub for version control.

> `https://github.com/uia-2026/is-202-programmeringsprosjekt`

## Where to start

| I want to...                         | Read                                           |
| ------------------------------------ | ---------------------------------------------- |
| Run the project on my machine        | [Getting started](./getting-started.md)        |
| Understand how the system is built   | [Architecture](./architecture.md)              |
| Know why we chose X over Y           | [Design decisions](./design-decisions.md)      |
| Look at the tables and relationships | [Database](./database.md)                      |
| See how login and two-factor work    | [Authentication](./features/authentication.md) |
| See how matching works               | [Matching](./features/matching.md)             |
| See how the map works                | [Map](./features/map.md)                       |
| See how photo uploads work           | [Attachments](./features/attachments.md)       |
| Deploy the application               | [Deployment](./deployment.md)                  |
| See what was tested and the results  | [Testing](./testing.md)                        |
| See how we used AI                   | [AI usage](./ai-usage.md)                      |

## Technology at a glance

| Area                | Choice                                                                                          |
| ------------------- | ----------------------------------------------------------------------------------------------- |
| Web framework       | ASP.NET Core MVC with Razor views, .NET 10                                                      |
| Database            | MariaDB 11 (Entity Framework Core with the Pomelo provider)                                     |
| Map                 | Leaflet 1.9 with OpenStreetMap tiles, addresses from Geonorge                                   |
| Authentication      | Cookie authentication, passwords hashed with the ASP.NET Core `PasswordHasher`, TOTP two-factor |
| File storage        | S3-compatible object storage (Supabase Storage)                                                 |
| Local orchestration | .NET Aspire (AppHost, Migrator, Web, MariaDB)                                                   |
| Tests               | xUnit and Moq, run by GitHub Actions on every push and pull request                             |
| Documentation       | VitePress, with the source readable as plain Markdown on GitHub                                 |
