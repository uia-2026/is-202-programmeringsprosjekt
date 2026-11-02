# Deployment Architecture

## Overview

Heimevernet is hosted on a single Azure virtual machine. The application
and its supporting services run as Docker containers and are managed by
Docker Compose. Deployments are triggered automatically whenever changes
are pushed to the `main` branch.

The deployment consists of four containerized services:

- **Caddy** is the public entry point. It handles HTTPS and forwards
  incoming requests to the ASP.NET Core application.
- **App** runs the ASP.NET Core web application.
- **Migrator** applies Entity Framework Core database migrations.
- **Database** runs MariaDB 11 and stores the application's relational
  data.

Attachments are stored separately in an S3-compatible object storage
service.

## Architecture

```text
                         Internet
                            |
                 heimevernet.duckdns.org
                            |
                      ports 80 / 443
                            |
                 +----------v-----------+
                 |        Caddy         |
                 | reverse proxy / TLS  |
                 +----------+-----------+
                            |
                       http://app:8080
                            |
                 +----------v-----------+       +----------------------+
                 |         App          +------>| S3-compatible object |
                 |    ASP.NET Core      |       | storage (attachments)|
                 +----------+-----------+       +----------------------+
                            |
                 +----------v-----------+
                 |      MariaDB 11      |
                 |      Database       |
                 +----------^-----------+
                            |
                 +----------+-----------+
                 |       Migrator       |
                 |    EF Core schema    |
                 |      migrations      |
                 +----------------------+
```

All four containers are connected through the Docker Compose network.
Only Caddy publishes ports to the virtual machine's host network. The
application and database are not directly exposed to the internet.

The application communicates with MariaDB through the internal Docker
network. It uses the external object storage service for file
attachments.

## Azure Virtual Machine

The deployment runs on the following Azure infrastructure:

Setting Value

---

Name `heimevernet-vm`
Resource group `heimevernet_group`
Region Spain Central
Size `Standard_B2als_v2` (2 vCPU, 4 GiB RAM)
Operating system Ubuntu Server 24.04 LTS
OS disk 30 GB Premium SSD (`Premium_LRS`)
Authentication SSH key only; password login is disabled
Admin user `danylo-bodnar`

The virtual machine hosts the Docker environment and the application
containers. The database files and Caddy state are stored in
Docker-managed volumes on the VM.

The operating system disk has a capacity of 30 GB. Docker images and
other container data use this disk, so image accumulation is a relevant
storage constraint.

## Services

### Caddy

Caddy acts as the reverse proxy and the only public entry point to the
application.

It listens on ports 80 and 443. Incoming requests for
`heimevernet.duckdns.org` are forwarded to the application container at
`http://app:8080`.

Caddy also handles TLS termination. It obtains and renews HTTPS
certificates automatically through Let's Encrypt. Certificate and
configuration state are persisted in Docker volumes.

The application itself communicates over HTTP inside the private Docker
network. HTTPS is handled at the reverse proxy rather than directly by
ASP.NET Core.

### ASP.NET Core application

The `app` container runs the Heimevernet ASP.NET Core web application.

The application listens on port 8080 inside the Docker network. It is
not published directly to the host, so external HTTP and HTTPS traffic
reaches it through Caddy.

The application uses MariaDB for relational data and an S3-compatible
object storage service for attachments. Its runtime configuration is
supplied through environment variables.

The application runs in the .NET 10 ASP.NET Core runtime image. The
Dockerfile uses a multi-stage build:

1.  The .NET SDK image restores and builds the project.
2.  The publish stage produces the application release output.
3.  The ASP.NET Core runtime image hosts the published application.

The final runtime container runs as a non-root user.

### MariaDB database

The `db` container runs MariaDB 11 and stores the application's
relational data.

The database is reachable by the application and migrator through the
Docker Compose network. It is not exposed directly to the public
internet and does not publish port 3306 to the VM host.

Database files are stored in the persistent `mariadb_data` Docker
volume. This allows the database data to remain available when
containers are recreated or stopped, as long as the volume is retained.

The database name and credentials are provided through environment-based
configuration.

### Database migrator

The `migrator` container is responsible for applying Entity Framework
Core migrations to the MariaDB database.

It runs as a separate process from the web application. Once the
migrations have been applied, the migrator exits rather than remaining
running as a long-lived service.

The migrator waits for the database health check before applying
migrations.

In the current Docker Compose configuration, the application waits for
the database but does not wait for the migrator to finish. Consequently,
during a deployment that introduces a new migration, the application can
start briefly while the database still has its previous schema.

## Networking and DNS

The VM's Azure Network Security Group controls inbound network traffic.

---

Port Protocol Purpose

---

22 TCP SSH access, including
access used by GitHub
Actions

80 TCP HTTP traffic, Let's
Encrypt certificate
validation and HTTPS
redirection

443 TCP HTTPS traffic

443 UDP HTTP/3 traffic
supported by Caddy

---

Ports 3306 and 8080 are not exposed publicly. MariaDB and the ASP.NET
Core application are accessed through the internal Docker network.

The application is available through the DuckDNS hostname:

`heimevernet.duckdns.org`

The hostname's DNS A record points to the public IP address of the Azure
VM. Caddy uses this hostname when obtaining and renewing the TLS
certificate. If the VM's public IP address changes, the DNS record must
point to the new address for the domain and certificate management to
continue working correctly.

A static public IP address prevents the DNS record from becoming
outdated when the VM is deallocated.

## HTTPS and Reverse Proxy

Caddy is responsible for handling public HTTPS traffic.

The request flow is:

1.  A client connects to `heimevernet.duckdns.org` over HTTP or HTTPS.
2.  The request reaches Caddy on the VM.
3.  Caddy handles HTTPS and terminates the TLS connection.
4.  Caddy forwards the request to `app:8080` over the internal Docker
    network.
5.  The ASP.NET Core application processes the request and returns a
    response through Caddy.

The Caddy configuration identifies the application container as the
reverse proxy target:

```text
heimevernet.duckdns.org {
    reverse_proxy app:8080
}
```

Caddy automatically obtains and renews certificates through Let's
Encrypt. Its certificate data and configuration are persisted in the
`caddy_data` and `caddy_config` volumes. The certificate data is
important because removing it causes Caddy to request certificates
again.

## Configuration and Secrets

Docker Compose reads deployment-specific values from an `.env` file
located alongside the Compose configuration. This file is not committed
to the Git repository.

The configuration includes database credentials and the connection
details for the S3-compatible object storage service.

### Database configuration

The database configuration is supplied through the following variables:

Variable Purpose

---

`MYSQL_ROOT_PASSWORD` MariaDB root password
`MYSQL_DATABASE` Application database name
`MYSQL_USER` Application database user
`MYSQL_PASSWORD` Application database user's password

The application receives its database connection string through
`ConnectionStrings__heimevernetdb`. ASP.NET Core maps double underscores
in environment variable names to colons in configuration keys.

The database hostname used by the application is `db`, which resolves to
the MariaDB container through the Docker Compose network.

### Object storage configuration

Attachments are stored in an external S3-compatible object storage
service. The application uses the following settings:

---

Variable Purpose

---

`STORAGE_ENDPOINT` Object storage endpoint

`STORAGE_REGION` Object storage region

`STORAGE_ACCESS_KEY_ID` Access key identifier

`STORAGE_SECRET_ACCESS_KEY` Secret access key

`STORAGE_BUCKET` Bucket used for attachments

`STORAGE_PUBLIC_BASE_URL` Public base URL used to link to
uploaded files

---

The corresponding ASP.NET Core configuration keys use the `Storage`
section, such as `Storage__Endpoint` and `Storage__Bucket`.

### GitHub Actions secret

The deployment workflow uses the `VM_SSH_PRIVATE_KEY` GitHub Actions
secret to authenticate to the Azure VM over SSH.

The secret contains the private key corresponding to a public key
authorized on the VM. The private key is kept in GitHub Actions secrets
rather than committed to the repository.

## Persistent Storage

The deployment uses Docker volumes for data that must survive container
recreation.

Volume Contents

---

`mariadb_data` MariaDB database files
`caddy_data` TLS certificates and Caddy state
`caddy_config` Caddy's autosaved configuration

The database volume contains the relational application data. The Caddy
volumes preserve certificate and configuration state.

Attachments are not stored in the MariaDB database volume. They are
stored in the external S3-compatible object storage bucket. The database
dump therefore does not include the uploaded attachment files.

The VM's 30 GB OS disk is also used for Docker images and container
data. Since deployments can create updated images, accumulated unused
images can consume disk space.

## CI/CD Pipeline

Deployment is automated through GitHub Actions. The workflow is defined
in `.github/workflows/deploy.yml` and is triggered by pushes to the
`main` branch.

The workflow connects to the Azure VM over SSH and runs the deployment
process from the repository directory on the server.

The deployment process consists of the following stages:

1.  **SSH connection:** GitHub Actions connects to the VM using the
    `VM_SSH_PRIVATE_KEY` repository secret.
2.  **Source update:** The workflow runs `git pull origin main` to
    retrieve the latest version of the repository.
3.  **Container build and update:** Docker Compose builds the
    application images and recreates containers whose configuration or
    image has changed.
4.  **Database migration:** The migrator container applies any pending
    Entity Framework Core migrations.
5.  **Service continuation:** Caddy and the database remain running
    unless their configuration or image has changed.

The workflow uses the `appleboy/ssh-action` action to execute the remote
deployment commands.

The deployment is therefore based on the current state of the `main`
branch. A push to another branch does not trigger this workflow.

## Database Migrations

Entity Framework Core migrations are applied by the dedicated migrator
container.

The migrator connects to MariaDB, waits for the database health check to
pass, applies pending migrations and exits. This separates database
schema changes from the normal runtime responsibilities of the web
application.

The application and migrator share the database configuration and
communicate with the database through the Docker network.

One deployment consideration is the current startup dependency
configuration: the application waits for the database but not for the
migrator to complete. A deployment containing a schema change can
therefore have a short interval where the application is running against
the previous database schema.

## Backups and Recovery

The MariaDB database contains relational application data and is
separate from the external object storage used for attachments.

A database backup created from MariaDB contains the database contents,
but does not include files stored in the S3-compatible bucket. A
complete recovery strategy therefore needs to account for both the
database and the external attachment storage.

A backup stored only on the VM does not protect against loss of that VM
or its disk. Database backups need to be stored separately from the VM
to provide protection against infrastructure loss.

Deployments use Git commits as their application source. An earlier
commit can be deployed as the application version, but database
migrations are not automatically reversed when application code is
rolled back. If a migration has changed the schema incompatibly,
recovery may require restoring a database backup or applying a
corrective migration.

## Deployment Constraints

The current deployment has several relevant constraints:

- **Single VM:** The application, database, migrator and reverse proxy
  run on one virtual machine. The deployment does not provide VM-level
  redundancy.
- **Disk capacity:** The VM has a 30 GB OS disk. Docker images and
  container data share this storage.
- **Database exposure:** MariaDB is available only inside the Docker
  network, not directly from the public internet.
- **Attachment storage:** Uploaded files are stored outside the VM in
  S3-compatible object storage.
- **DNS dependency:** The DuckDNS A record must point to the VM's
  current public IP address for the domain to reach the deployment and
  for Caddy's certificate management to work.
- **Rollback behavior:** Reverting application code does not
  automatically reverse database migrations.
