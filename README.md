# TaskFlow — Task & Project Management

A full-stack task and project management app built as a learning/portfolio project, demonstrating a
production-shaped architecture end to end: Angular frontend, ASP.NET Core Web API backend, PostgreSQL,
JWT auth, Docker Compose, and CI.

Users can register, log in, create projects, add tasks to them, and track task status on a drag-and-drop
kanban board. Each user only sees and manages their own data.

## Tech stack

| Layer          | Technology |
|----------------|------------|
| Frontend       | Angular 22 (standalone components), Tailwind CSS, RxJS, Angular CDK (drag-drop) |
| Backend        | ASP.NET Core 8 Web API, C#, EF Core (code-first migrations) |
| Database       | PostgreSQL 16 |
| Auth           | JWT access + refresh tokens, BCrypt password hashing |
| Containerization | Docker + Docker Compose |
| CI/CD          | GitHub Actions (separate backend/frontend workflows) |

## Repository structure

```
demoproject/
├── backend/                 ASP.NET Core Web API (layered architecture)
│   ├── src/
│   │   ├── TaskManager.Api/             Controllers, middleware, Program.cs, Swagger
│   │   ├── TaskManager.Application/     Services, DTOs, validators, interfaces
│   │   ├── TaskManager.Domain/          Entities, enums
│   │   └── TaskManager.Infrastructure/  EF Core DbContext, repositories, JWT token service, migrations
│   └── tests/
│       └── TaskManager.UnitTests/       xUnit unit tests (services, validators)
├── frontend/                 Angular application
│   └── src/app/
│       ├── core/             Guards, interceptors, singleton services (auth, projects, tasks, dashboard)
│       ├── shared/           Reusable UI components, models (typed DTOs), utils
│       └── features/         Route-level feature areas: auth, dashboard, projects
├── docker/                   Backend/frontend Dockerfiles
├── docker-compose.yml         One-command orchestration of postgres + backend + frontend
├── .env.example               Template for local secrets/config (copy to .env)
└── .github/workflows/         CI: backend-ci.yml, frontend-ci.yml
```

## Architecture

```mermaid
flowchart LR
    subgraph Browser
        UI[Angular SPA]
    end

    subgraph "Docker Compose network"
        FE[frontend container :80<br/>static file server, no proxy]
        BE[ASP.NET Core API :8080<br/>Controllers → Services → Repositories]
    end

    subgraph "Your machine"
        DB[(PostgreSQL, running natively)]
    end

    UI -- "HTTP :4200" --> FE
    UI -- "HTTP :5000/api (direct, CORS)" --> BE
    BE -- "EF Core, via host.docker.internal" --> DB
```

There's no reverse proxy in front of these containers — the frontend serves the built Angular files as
plain static assets and the browser calls the backend's published port directly (CORS is configured on the
backend to allow it). PostgreSQL is **not** containerized here: it runs natively on your machine, and the
backend container reaches it via Docker's `host.docker.internal` DNS name. If you want a reverse proxy or
TLS in front of either service, that's on you to add (e.g. your own nginx/Caddy in front of both ports).

**Backend** follows a layered architecture:

- **Controllers** (`TaskManager.Api`) — thin, handle HTTP concerns only; extract the current user's id from
  the JWT and delegate to services.
- **Services** (`TaskManager.Application`) — business logic, ownership checks (a user can only touch their
  own projects/tasks), mapping between entities and DTOs.
- **Repositories** (`TaskManager.Infrastructure`) — EF Core data access behind interfaces defined in
  `Application`, so services are unit-testable with mocked repositories.
- **DTOs** — the API never returns EF entities directly; request/response shapes are explicit records.
- A global **exception-handling middleware** converts domain exceptions (`NotFoundException`,
  `ForbiddenAccessException`, `ValidationAppException`, `AuthenticationException`) into consistent JSON
  error responses with the right HTTP status codes.
- **FluentValidation** validators run server-side on every write request via an action filter — the API
  never trusts client-side validation alone.

**Frontend** follows a feature-based structure:

- **`core/`** — route guards (`authGuard`, `guestGuard`), an HTTP interceptor that attaches the JWT to
  outgoing requests and transparently refreshes an expired access token on a 401 (queuing concurrent
  requests during the refresh), and the singleton API services.
- **`shared/`** — presentational components (badges, spinner, empty state, alert, task card) and TypeScript
  interfaces mirroring the backend DTOs.
- **`features/`** — lazy-loaded route-level components: auth (login/register), dashboard, and the project
  detail kanban board (built with Angular CDK drag-and-drop).

**Auth flow**: on login/register the API returns a short-lived access token and a longer-lived refresh
token. The frontend stores both in `localStorage` and attaches the access token as a Bearer header. When a
request comes back `401`, the interceptor calls `/api/auth/refresh` once, retries the original request with
the new token, and — if the refresh itself fails — clears the session and redirects to `/login`.

## Database schema

- **Users** — `Id, Email (unique), PasswordHash, DisplayName, CreatedAt`
- **RefreshTokens** — `Id, UserId (FK), TokenHash (unique), ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash`
- **Projects** — `Id, UserId (FK), Name, Description, CreatedAt, UpdatedAt`
- **ProjectTasks** — `Id, ProjectId (FK), Title, Description, Status (Todo/InProgress/Done), Priority (Low/Medium/High), DueDate, CreatedAt, UpdatedAt`

Relations: `User 1—N Project`, `Project 1—N ProjectTask`, `User 1—N RefreshToken`, all cascade-deleted.

## API endpoints

| Method | Path | Description |
|---|---|---|
| POST | `/api/auth/register` | Create an account, returns tokens |
| POST | `/api/auth/login` | Returns tokens |
| POST | `/api/auth/refresh` | Rotates the refresh token, returns a new access token |
| POST | `/api/auth/logout` | Revokes the refresh token |
| GET | `/api/projects` | List the current user's projects (with task status counts) |
| GET | `/api/projects/{id}` | Project detail |
| POST | `/api/projects` | Create a project |
| PUT | `/api/projects/{id}` | Update a project |
| DELETE | `/api/projects/{id}` | Delete a project (cascades its tasks) |
| GET | `/api/projects/{projectId}/tasks` | List tasks in a project |
| GET | `/api/projects/{projectId}/tasks/{taskId}` | Task detail |
| POST | `/api/projects/{projectId}/tasks` | Create a task |
| PUT | `/api/projects/{projectId}/tasks/{taskId}` | Update a task |
| PATCH | `/api/projects/{projectId}/tasks/{taskId}/status` | Change a task's status (used by the kanban board) |
| DELETE | `/api/projects/{projectId}/tasks/{taskId}` | Delete a task |
| GET | `/api/dashboard/summary` | Project list + aggregate task counts for the dashboard |

All endpoints except `/api/auth/*` require a `Bearer` access token and enforce that the authenticated user
owns the project/task being accessed (403 otherwise, 404 if it doesn't exist).

Full interactive API docs (Swagger/OpenAPI) are available at `/swagger` once the backend is running.

## Local PostgreSQL setup

This project does **not** run Postgres in a container — the backend container connects to PostgreSQL
running natively on your machine. You only need to do this once.

**macOS (Homebrew):**

```bash
brew install postgresql@16
brew services start postgresql@16
```

Create the app's database and user (change the password to whatever you'll put in `.env`):

```bash
createdb taskmanager
psql postgres -c "CREATE USER taskmanager WITH PASSWORD 'yourpassword';"
psql postgres -c "GRANT ALL PRIVILEGES ON DATABASE taskmanager TO taskmanager;"
psql -d taskmanager -c "GRANT ALL ON SCHEMA public TO taskmanager;"
```

Allow connections from Docker Desktop's internal network. Find your config files with:

```bash
psql postgres -c "SHOW config_file;"
psql postgres -c "SHOW hba_file;"
```

Edit `postgresql.conf` (the first path above) and make sure this line is uncommented and set to `*`:

```
listen_addresses = '*'
```

Edit `pg_hba.conf` (the second path above) and add this line (local-dev convenience — it allows password-
authenticated connections from any address, which is fine on a machine you control; narrow the CIDR later
if you want it tighter):

```
host    all             all             0.0.0.0/0               scram-sha-256
```

Restart Postgres for the changes to take effect:

```bash
brew services restart postgresql@16
```

**Windows / other setups:** same three steps — create the `taskmanager` DB/user, set `listen_addresses = '*'`
in `postgresql.conf`, and add the `host all all 0.0.0.0/0 scram-sha-256` line to `pg_hba.conf` — just adjust
the install/service commands and config file locations for your platform (on native Windows Postgres,
config files live under `C:\Program Files\PostgreSQL\<version>\data\`).

## Running it — Docker Compose (one command)

**Prerequisites:** Docker Desktop, and a local PostgreSQL set up per the section above and currently running.

```bash
cp .env.example .env
```

Open `.env` and set real values for `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` to match the
database/user you just created, and set `JWT_SECRET` to a long random string (the compose file refuses to
start without these — see the comments in `.env.example`). Then:

```bash
docker compose up --build
```

This builds and starts two containers — `backend` and `frontend`. The backend reaches your local Postgres
via Docker's `host.docker.internal` DNS name and automatically applies EF Core migrations on startup (no
manual migration step needed). There's no nginx or reverse proxy — the frontend serves static files
directly and calls the backend on its own published port.

Once everything is healthy:

- **App:** http://localhost:4200 (or whatever `FRONTEND_PORT` you set)
- **API + Swagger:** http://localhost:5000/swagger (or whatever `BACKEND_PORT` you set)

Register an account, create a project, add a task, drag it across the kanban columns, and log out — that's
the full golden path, no manual steps beyond `docker compose up` (with local Postgres already running).

### Demo account (seeded data)

On startup the backend seeds a demo account **only if the `Users` table is empty**, so a fresh database
comes up with something to look at instead of a blank dashboard:

| Email | Password |
|---|---|
| `demo@taskflow.app` | `Demo123!` |

It owns 3 projects ("Website Redesign", "Mobile App Launch", "Q3 Marketing Campaign") with 15 tasks
spread across the Todo / In Progress / Done columns. The seeder
([`DbSeeder`](backend/src/TaskManager.Infrastructure/Persistence/DbSeeder.cs), called from
`Program.cs` after migrations) is a no-op once any user exists, so it never overwrites real data — to
re-seed, empty the `Users` table (cascades to projects/tasks) and restart the backend. This is a
convenience for local/demo use; remove the `DbSeeder.SeedAsync` call for a real deployment.

To stop: `docker compose down`.

**If the backend keeps restarting / can't connect to the database:** double-check Postgres is running
(`brew services list` on macOS), that the `.env` credentials match what you created above, and that the
`pg_hba.conf`/`listen_addresses` changes were actually applied (`brew services restart postgresql@16` after
editing them). `docker compose logs backend` will show the connection error.

## Running it locally without Docker (development)

**Backend** (requires .NET 8 SDK and a local PostgreSQL instance):

```bash
cd backend
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=taskmanager;Username=taskmanager;Password=<yourpassword>"
export Jwt__Secret="<a long random string, 32+ chars>"
dotnet run --project src/TaskManager.Api
```

The API listens on `http://localhost:5000` and serves Swagger at `/swagger`. Migrations are applied
automatically on startup.

**Frontend** (requires Node.js 22.22.3+):

```bash
cd frontend
npm install
npm start
```

Serves at `http://localhost:4200` and points at `http://localhost:5000/api` in development
(`src/environments/environment.development.ts`).

## Running the tests

```bash
# Backend — 16 xUnit tests covering auth, ownership authorization, and validators
cd backend && dotnet test

# Frontend — vitest unit tests covering services, guard, interceptor, and a component
cd frontend && npx ng test

# Frontend lint (angular-eslint)
cd frontend && npx ng lint
```

Both suites also run automatically in CI on every push that touches their respective directory
(`.github/workflows/backend-ci.yml`, `.github/workflows/frontend-ci.yml`).

## Secrets handling

No secrets are committed to this repository. `.env` is git-ignored; `.env.example` documents every
variable with safe placeholder values. `appsettings.json` ships with empty `ConnectionStrings` and
`Jwt:Secret` — the app deliberately fails fast if they aren't supplied via environment variables (Docker
Compose) or a local, git-ignored `appsettings.Development.json` / shell environment.

## What's implemented vs. what a real production deployment would still need

**Implemented:**
- JWT access + refresh token auth with rotation and revocation, BCrypt password hashing
- Server-side authorization (ownership checks), not just UI hiding
- Server-side validation (FluentValidation) independent of client-side validation
- Layered backend architecture with DTOs, a global error-handling middleware, Swagger docs
- Feature-based Angular structure, route guards, an auth interceptor with automatic token refresh
- Loading / error / empty states throughout, responsive layout
- Unit tests on both sides proving the pattern (not exhaustive coverage)
- Docker Compose bringing up the app containers with one command against your local Postgres, with health
  checks and automatic migrations
- CI running build + lint + test on every push, split by frontend/backend

**Still needed for a real production deployment:**
- **Secret management** — `.env` files are fine for local dev; production should pull secrets from a
  managed secret store (Azure Key Vault, AWS Secrets Manager, Doppler, etc.), not files on disk.
- **HTTPS/TLS** — both containers currently serve plain HTTP with no reverse proxy in front of them;
  production needs TLS termination (a load balancer, nginx/Caddy/Traefik with automatic certs, or a cloud
  provider's managed HTTPS) and a managed/containerized Postgres instead of one running on a developer machine.
- **Logging & monitoring** — no structured logging sink, metrics, tracing, or alerting is wired up
  (e.g. Serilog + an aggregator, Application Insights/Datadog/Grafana, uptime checks).
- **Refresh token storage** — tokens live in `localStorage`, which is simple but vulnerable to XSS token
  theft; a production build would likely move the refresh token to an httpOnly cookie.
- **Rate limiting / brute-force protection** on auth endpoints.
- **Database migrations strategy** for zero-downtime deploys (currently migrations run automatically on
  every container start, which is convenient for a demo but risky with multiple replicas in production).
- **A real deployment target** — e.g. Azure App Service/Container Apps, AWS ECS/Fargate, or a Kubernetes
  cluster, plus a CD pipeline that builds images, pushes to a registry, and deploys them (this repo's CI
  only builds/tests; it doesn't deploy anywhere).
- **Backups & disaster recovery** for the Postgres data volume.
- **CORS lockdown** — currently allows `localhost`; production should restrict `Cors:AllowedOrigins` to the
  real frontend domain(s).
- **Horizontal scaling considerations** — e.g. sticky sessions are not needed here (JWTs are stateless), but
  the Data Protection key ring warning in the logs indicates keys aren't persisted across container
  restarts/replicas, which would invalidate anything relying on it in a multi-instance deployment.
