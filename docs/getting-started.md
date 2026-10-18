# Getting started

This page takes a fresh clone to a running application.

## Prerequisites

- .NET 10 SDK
- Docker Desktop, running
- Git
- Node.js 22 (only to preview this documentation)

## 1. Clone

```bash
git clone https://github.com/uia-2026/is-202-programmeringsprosjekt
cd is-202-programmeringsprosjekt
```

## 2. Set the secrets

Secrets are stored with .NET user secrets and are never committed. Without them the application does not start.

**AppHost: password for the local MariaDB root user is taken from appsetings.Development.json**

```bash
  "Parameters": {
    "mariadb-password": "dev_password_123"
  },
```

**Web application and Migrator: object storage settings.** Use the same values in both projects. Ask a teammate for the development values.

```bash
cd ../Heimevernet
dotnet user-secrets set "Storage:Endpoint" "<s3 endpoint>"
dotnet user-secrets set "Storage:Region" "<region>"
dotnet user-secrets set "Storage:AccessKeyId" "<access key id>"
dotnet user-secrets set "Storage:SecretAccessKey" "<secret>"
dotnet user-secrets set "Storage:Bucket" "<dev bucket name>"
dotnet user-secrets set "Storage:PublicBaseUrl" "<public url prefix of the bucket>"
```

| Key                                              | Meaning                                                               |
| ------------------------------------------------ | --------------------------------------------------------------------- |
| `Storage:Endpoint`                               | S3 API endpoint of the storage provider                               |
| `Storage:Region`                                 | Region name used to sign requests                                     |
| `Storage:AccessKeyId`, `Storage:SecretAccessKey` | Credentials for the bucket                                            |
| `Storage:Bucket`                                 | Bucket name. Use the dev bucket locally, never the production bucket  |
| `Storage:PublicBaseUrl`                          | URL prefix that turns a stored object key into a public image address |

Both projects validate these settings. If one is missing, the Migrator or the Web app fails and the AppHost dashboard shows the resource as failed.

## 3. Run

```bash
dotnet run --project Heimevernet.AppHost
```

Aspire starts MariaDB, then the Migrator (which creates the schema and seeds data), then the web application. The console prints the Aspire dashboard address. The dashboard lists the endpoint of the web application.`.

## 4. Development accounts

The Migrator creates two accounts in the Development environment only:

| Role              | Email                               | Password          |
| ----------------- | ----------------------------------- | ----------------- |
| Public actor      | `dev.public.actor@example.com`      | `DevPassword123!` |
| Resource provider | `dev.resource.provider@example.com` | `DevPassword123!` |

Two-factor is off for them until it is set up from the account pages.

## Connect to the database

Use any database tool, for example DataGrip.

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
2. In Docker Desktop, open Volumes and delete the MariaDB volume that belongs to this project.
3. Start the AppHost again. The Migrator recreates the schema, categories and development data.

Each reseed uploads two sample photos to the dev bucket, so a few unused files accumulate there. This is harmless and can be cleaned up by hand.

## Run the tests

```bash
dotnet test Heimevernet.slnx
```

## Preview the documentation

```bash
npm install
npm run docs:dev
```
