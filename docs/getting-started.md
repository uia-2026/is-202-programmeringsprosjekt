# Getting started

This page takes a fresh clone to a running application in about ten minutes.

::: tip Just want to look around?
The live demo at [heimevernet.duckdns.org](https://heimevernet.duckdns.org) needs no setup. Log in with one of the [development accounts](#_4-log-in).
:::

## Prerequisites

| Tool                                                                           | Why                                       |
| ------------------------------------------------------------------------------ | ----------------------------------------- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download)                           | Builds and runs all projects              |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/), **running** | Aspire starts MariaDB in a container      |
| Git                                                                            | Clone the repository                      |
| Node.js 22 (optional)                                                          | Only needed to preview this documentation |

Check that everything is in place:

```bash
dotnet --version   # 10.x
docker info        # must not print an error
```

## 1. Clone

```bash
git clone https://github.com/uia-2026/is-202-programmeringsprosjekt
cd is-202-programmeringsprosjekt
```

All later commands run from this folder.

## 2. Set the storage secrets

Photos are stored in S3-compatible object storage. Its credentials are kept in .NET user secrets on your machine and are **never committed**. The web application and the Migrator refuse to start without them.

The AppHost, the web application and the Migrator share one user-secrets store, so you set the values **once**.

::: info Where do I get the values?
**Graders:** the ready-to-paste commands come with the submission. Paste them into a terminal in the repository folder, then continue with [step 3](#_3-run).
**Team members:** ask a teammate for the development values.
:::

The commands have this shape:

```bash
dotnet user-secrets set "Storage:Endpoint" "<s3 endpoint>" --project Heimevernet
dotnet user-secrets set "Storage:Region" "<region>" --project Heimevernet
dotnet user-secrets set "Storage:AccessKeyId" "<access key id>" --project Heimevernet
dotnet user-secrets set "Storage:SecretAccessKey" "<secret>" --project Heimevernet
dotnet user-secrets set "Storage:Bucket" "<dev bucket name>" --project Heimevernet
dotnet user-secrets set "Storage:PublicBaseUrl" "<public url prefix of the bucket>" --project Heimevernet
```

Check the result with `dotnet user-secrets list --project Heimevernet`.

| Key                                              | Meaning                                                               |
| ------------------------------------------------ | --------------------------------------------------------------------- |
| `Storage:Endpoint`                               | S3 API endpoint of the storage provider                               |
| `Storage:Region`                                 | Region name used to sign requests                                     |
| `Storage:AccessKeyId`, `Storage:SecretAccessKey` | Credentials for the bucket                                            |
| `Storage:Bucket`                                 | Bucket name. Use the dev bucket locally, never the production bucket  |
| `Storage:PublicBaseUrl`                          | URL prefix that turns a stored object key into a public image address |

The local MariaDB password needs no setup. It is a development-only value in `Heimevernet.AppHost/appsettings.Development.json`.

## 3. Run

```bash
dotnet run --project Heimevernet.AppHost
```

Aspire starts the services in this order:

1. **MariaDB** in Docker (the first run downloads the image, which takes a minute)
2. **Migrator**: creates the schema and seeds categories and development data, then exits
3. **Web application**

The console prints a link to the Aspire dashboard (it contains a login token). Open it, wait until `heimevernet` shows **Running**, and click its endpoint to open the application.

## 4. Log in

The Migrator creates these accounts in the Development environment only:

| Role              | Email                               | Password          |
| ----------------- | ----------------------------------- | ----------------- |
| Public actor      | `dev.public.actor@example.com`      | `DevPassword123!` |
| Resource provider | `dev.resource.provider@example.com` | `DevPassword123!` |

Two-factor authentication is off for these accounts until it is set up from the account pages.

## Troubleshooting

| Symptom                                                                                      | Cause and fix                                                                                                          |
| -------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| AppHost fails right away with a container error                                              | Docker Desktop is not running. Start it and run again.                                                                 |
| `migrator` or `heimevernet` shows **Failed**, log says `Storage configuration is incomplete` | One or more storage secrets are missing. Run `dotnet user-secrets list --project Heimevernet` and compare with step 2. |
| MariaDB fails to start, port in use                                                          | Something else uses port `3307`. Stop it, or change `port:` in `Heimevernet.AppHost/AppHost.cs`.                       |
| Images do not load, everything else works                                                    | `Storage:PublicBaseUrl` is wrong or the bucket is not public.                                                          |

## Connect to the database

Use any database tool, for example DataGrip or DBeaver.

| Setting  | Value              |
| -------- | ------------------ |
| Host     | `localhost`        |
| Port     | `3307`             |
| User     | `root`             |
| Password | `dev_password_123` |
| Database | `heimevernetdb`    |

The database lives in a persistent Docker volume, so data survives restarts.

## Reset the local database

1. Stop the AppHost.
2. In Docker Desktop, open **Volumes** and delete the MariaDB volume that belongs to this project.
3. Start the AppHost again. The Migrator recreates the schema, categories and development data.

Each reseed uploads two sample photos to the dev bucket, so a few unused files pile up there. This is harmless and they can be removed by hand.

## Run the tests

```bash
dotnet test Heimevernet.slnx
```

## Preview the documentation

```bash
npm install
npm run docs:dev
```
