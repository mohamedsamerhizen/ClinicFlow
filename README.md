# ClinicFlow

ClinicFlow is a portfolio-grade Smart Clinic Management System API built with .NET 9, ASP.NET Core Web API, Entity Framework Core, SQL Server, ASP.NET Core Identity, JWT authentication, Serilog, and Swagger.

The project models realistic clinic workflows: doctors, patients, schedules, appointments, visits, prescriptions, dashboard reporting, audit fields, soft delete, role-based authorization, validation, and automated tests.

## Portfolio Highlights

- JWT Authentication with ASP.NET Core Identity
- Refresh Tokens with hashed storage and rotation
- Role-based Authorization for Admin, Doctor, and Receptionist workflows
- EF Core migrations with SQL Server constraints and indexes
- SQL Server persistence for local and Docker environments
- Docker Compose setup for API and database services
- SQLite integration tests with xUnit and WebApplicationFactory
- GitHub Actions CI for restore, build, test, and format checks
- Health Checks, Rate Limiting, and response caching
- Serilog logging for structured diagnostics

## Tech Stack

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9
- SQL Server / SQL Server LocalDB
- ASP.NET Core Identity
- JWT Bearer Authentication
- Serilog
- Swagger / OpenAPI
- xUnit
- Docker Compose

## Architecture

ClinicFlow uses a pragmatic layered structure suitable for a portfolio backend:

```text
HTTP Request
  -> Controller
  -> Service
  -> AppDbContext
  -> SQL Server
```

```mermaid
flowchart LR
    Client["Client / Swagger / Postman"] --> Controllers["API Controllers"]
    Controllers --> Services["Application Services"]
    Services --> DbContext["EF Core AppDbContext"]
    DbContext --> Database["SQL Server"]
    Services --> Identity["ASP.NET Core Identity"]
    Controllers --> Middleware["Validation + Exception Middleware"]
```

## Modules

- Auth
- Doctors
- Patients
- Specializations
- Doctor Schedules
- Appointments
- Visits
- Prescriptions
- Dashboard

## Roles and Permissions

| Role | Typical Access |
|---|---|
| Admin | Full management, dashboard, users, doctors, schedules, delete operations |
| Receptionist | Patients and appointments workflow |
| Doctor | Own appointments, visits, prescriptions, and clinical workflow |

## Important Business Rules

Appointments:

- Appointment date must be in the future.
- Appointment time cannot include seconds or milliseconds.
- Appointment time must align to a 15-minute boundary.
- Appointment must be inside the doctor's working schedule.
- A doctor cannot have two active appointments at the same time.
- A patient cannot have two active appointments at the same time.
- Cancelled appointments can free the time slot.
- Database unique indexes protect against race-condition conflicts.

Deletion and soft delete:

- Doctors, patients, and specializations use soft delete.
- Query filters hide soft-deleted data from normal reads.
- Doctors cannot be deleted if linked schedules or appointments exist.
- Patients cannot be deleted if linked appointments exist.
- Visits cannot be deleted if linked prescriptions exist.
- Specializations cannot be deleted if linked doctors exist.

Clinical workflow:

- Visits cannot be created for cancelled or pending appointments.
- Visits cannot be created before the appointment time.
- Prescriptions must be linked to an existing visit.

## Project Structure

```text
Repository root
  Common/
  Constants/
  Controllers/
  Data/
    Seed/
  DTOs/
  Entities/
  Enums/
  Extensions/
  Interfaces/
  Middlewares/
  Migrations/
  Services/
  ClinicFlow.csproj
  ClinicFlow.slnx
  Program.cs
  appsettings.json

ClinicFlow.Tests/
  TestSupport/
  AppointmentServiceTests.cs
  ApiIntegrationTests.cs
  AuthIntegrationTests.cs
  VisitServiceTests.cs

.github/workflows/ci.yml
docs/
Dockerfile
docker-compose.yml
postman/ClinicFlow.postman_collection.json
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server LocalDB, SQL Server Developer, or Docker
- Optional: Docker Desktop

### Run Locally with LocalDB

From the repository root:

```powershell
dotnet restore ClinicFlow.slnx
dotnet build ClinicFlow.slnx
```

Configure development secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "ClinicFlow_Development_Jwt_Key_Change_Me_1234567890"
dotnet user-secrets set "AdminSeed:Enabled" "true"
dotnet user-secrets set "AdminSeed:FullName" "ClinicFlow Admin"
dotnet user-secrets set "AdminSeed:Email" "admin@clinicflow.local"
dotnet user-secrets set "AdminSeed:Password" "Admin@12345"
dotnet user-secrets set "DemoSeed:Enabled" "true"
```

Apply migrations and run:

```powershell
dotnet ef database update
dotnet run
```

Swagger is available in Development at:

```text
https://localhost:7093/swagger
http://localhost:5173/swagger
```

### Run with Docker Compose

Create a local `.env` from the sample:

```powershell
copy .env.example .env
```

Then run:

```powershell
docker compose up --build
```

The API will be available at:

```text
http://localhost:8080
http://localhost:8080/swagger
```

Default demo admin from `.env.example`:

```text
Email: admin@clinicflow.local
Password: Admin@12345
```

## Screenshots & Demo Flow

The screenshots below were captured from a local Docker run against `http://localhost:8080`.
Sensitive values such as passwords and token contents are redacted for public documentation.

| Screenshot | Path | What It Shows |
|---|---|---|
| Swagger UI | `docs/screenshots/swagger.png` | OpenAPI endpoint coverage |
| Health Check | `docs/screenshots/health.png` | `/health` returning a healthy response |
| Docker Run | `docs/screenshots/docker.png` | Docker Compose services running locally |
| Login Response | `docs/screenshots/postman-login.png` | Successful demo user login response |
| Refresh Token Response | `docs/screenshots/postman-refresh.png` | Refresh token rotation response |
| Appointments Workflow | `docs/screenshots/appointments.png` | Authenticated appointments response |

<p>
  <img src="docs/screenshots/swagger.png" alt="Swagger UI screenshot" width="720">
</p>

<p>
  <img src="docs/screenshots/health.png" alt="Health check screenshot" width="720">
</p>

<p>
  <img src="docs/screenshots/docker.png" alt="Docker Compose screenshot" width="720">
</p>

<p>
  <img src="docs/screenshots/postman-login.png" alt="Login response screenshot" width="720">
</p>

<p>
  <img src="docs/screenshots/postman-refresh.png" alt="Refresh token response screenshot" width="720">
</p>

<p>
  <img src="docs/screenshots/appointments.png" alt="Appointments workflow screenshot" width="720">
</p>

Local URLs:

```text
API:     http://localhost:8080
Swagger: http://localhost:8080/swagger
Health:  http://localhost:8080/health
```

Useful demo commands:

```powershell
docker compose up --build -d
docker compose down
dotnet build ClinicFlow.slnx
dotnet test ClinicFlow.slnx
```

Suggested demo flow:

1. Start Docker with `docker compose up --build -d`.
2. Open Swagger at `http://localhost:8080/swagger`.
3. Check `/health` at `http://localhost:8080/health`.
4. Login as the demo user.
5. Refresh the token.
6. Create a patient.
7. Create a doctor schedule.
8. Create an appointment.
9. Confirm the appointment.
10. Create a visit.
11. Add a prescription.
12. View the dashboard.

## Testing

Run all tests:

```powershell
dotnet test ClinicFlow.slnx
```

Current coverage includes:

- Appointment business rules
- Soft delete behavior
- SQLite-backed integration tests for relational constraints
- Doctor-scoped appointment visibility
- Visit creation rules
- API endpoint authorization
- Auth login/register/refresh/revoke integration tests
- Health/root endpoint checks
- `/api/v1` route compatibility

## CI

GitHub Actions workflow:

```text
.github/workflows/ci.yml
```

The pipeline runs:

- restore
- build
- tests
- format check

## Postman

Import this collection:

```text
postman/ClinicFlow.postman_collection.json
```

Set `baseUrl`, run `Auth - Login`, then use the authenticated requests.

## Configuration

Important settings:

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Key` | JWT signing key, minimum 32 characters |
| `Jwt:Issuer` | Token issuer |
| `Jwt:Audience` | Token audience |
| `Jwt:DurationInMinutes` | Token lifetime |
| `Jwt:RefreshTokenDurationInDays` | Refresh token lifetime |
| `AdminSeed:*` | Optional development admin seed |
| `DemoSeed:Enabled` | Optional development demo data |

Security notes:

- Never commit production secrets.
- The default JWT placeholder is blocked outside Development and Testing.
- Startup migrations and seed data run only in Development.
- Production deployments should apply migrations explicitly through CI/CD or an operator command.

## Useful Commands

```powershell
dotnet restore ClinicFlow.slnx
dotnet build ClinicFlow.slnx --no-restore
dotnet test ClinicFlow.slnx --no-build --no-restore
dotnet format ClinicFlow.slnx --verify-no-changes --no-restore
dotnet ef database update --project ClinicFlow.csproj
docker compose up --build -d
docker compose down
```

## API Examples

Login:

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@clinicflow.local",
  "password": "Admin@12345"
}
```

Refresh token:

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "userId": "{userId-from-login}",
  "refreshToken": "{refreshToken-from-login}"
}
```

Create an appointment:

```http
POST /api/appointments
Authorization: Bearer {token}
Content-Type: application/json

{
  "doctorId": 1,
  "patientId": 1,
  "appointmentDate": "2026-06-01T09:00:00"
}
```

Create a visit:

```http
POST /api/visits
Authorization: Bearer {token}
Content-Type: application/json

{
  "appointmentId": 1,
  "symptoms": "Headache and fatigue",
  "diagnosis": "Migraine",
  "notes": "Patient advised to rest and hydrate."
}
```

## Portfolio Notes

This project is intentionally practical rather than over-engineered. It keeps a clear service layer, strong validation, EF Core constraints, and integration tests while avoiding unnecessary architectural ceremony. It is designed to be easy for a reviewer to run, inspect, and discuss in an interview.
