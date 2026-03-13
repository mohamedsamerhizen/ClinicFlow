# ClinicFlow

Smart Clinic Management System API built with **.NET 9**, **ASP.NET Core Web API**, **Entity Framework Core**, **SQL Server LocalDB**, **ASP.NET Core Identity**, and **JWT Authentication**.

ClinicFlow is a portfolio-grade backend project designed to simulate a real-world clinic management system. It focuses on clean API design, role-based access control, business rule enforcement, auditability, soft delete support, validation, and production-oriented backend features.

---

## Features

### Authentication & Authorization
- ASP.NET Core Identity
- JWT Authentication
- Role-based Authorization
- Supported roles:
  - Admin
  - Doctor
  - Receptionist

### Core Modules
- Specializations
- Doctors
- Patients
- Doctor Schedules
- Appointments
- Visits
- Prescriptions
- Dashboard Overview

### Query & Reporting Features
- Pagination
- Search
- Filtering
- Patient Summary
- Patient History
- Upcoming Appointments
- Doctor Daily Schedule
- Dashboard Recent Activity

### Business Rules
- Prevent deleting a doctor if linked schedules or appointments exist
- Prevent deleting a patient if linked appointments exist
- Prevent deleting a visit if linked prescriptions exist
- Prevent deleting a specialization if linked doctors exist
- Prevent duplicate specialization names
- Prevent duplicate doctor phone numbers
- Prevent duplicate patient phone numbers
- Appointment validation rules:
  - Cannot create appointments in the past
  - Seconds and milliseconds are not allowed
  - Only 15-minute intervals are allowed
  - Conflict detection for both doctor and patient
  - Appointment must fall within one of the doctor’s actual working shifts

### Data & Persistence
- EF Core with SQL Server LocalDB
- Code First Migrations
- Database indexes and constraints
- Auto-applied migrations at startup

### Audit & Deletion Strategy
- Audit fields:
  - `CreatedAtUtc`
  - `CreatedByUserId`
  - `UpdatedAtUtc`
  - `UpdatedByUserId`
- Soft delete implemented for:
  - Doctors
  - Patients
  - Specializations
- Query filters configured to hide soft-deleted data from normal reads

### Validation & API Consistency
- DTO-based input validation using Data Annotations
- Unified validation error response format
- Unified API response wrapper
- Consistent status codes
- `CreatedAtAction` used for creation endpoints where applicable

### Production-Oriented Features
- Serilog logging
- Global exception handling middleware
- Health checks endpoint
- Rate limiting
- Response caching support
- Demo seed data
- Admin account seeding

---

## Tech Stack

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Core Identity
- JWT Bearer Authentication
- Swagger / OpenAPI
- Serilog

---

## Project Structure

```text
ClinicFlow
├── Common
├── Constants
├── Controllers
├── Data
│   └── Seed
├── DTOs
├── Entities
├── Enums
├── Extensions
├── Interfaces
├── Middlewares
├── Migrations
├── Services
├── Program.cs
├── appsettings.json
└── ClinicFlow.csproj
