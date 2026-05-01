---
applyTo: "User/**"
description: "Use when working on the User Service — gRPC service managing user identity, profiles, roles, delivery info, and credential verification."
---

# User Service

## Overview

gRPC service that owns user identity and profile data. Handles user creation, updates, credential verification, role management, and delivery address management. Called by Auth service for identity operations.

## Architecture (Clean Architecture)

- **Api/** — gRPC entry point (`UserGrpcService`), Mappers (proto ↔ domain via extension methods)
- **Application/** — Use cases: CreateUser, UpdateUser, GetUserById, GetUserByEmail, VerifyCredentials, AssignRole, RevokeRole, etc.
- **Domain/** — Rich domain model: UserEntity (with nested DeliveryInfo, Role, UserRestriction), Repositories, Common enums
- **Infrastructure/** — DbContext with audit/normalization hooks, Repositories, EF Configurations (ModelBuilder)
- **Protos/** — gRPC definitions for User and Role services

## Tech Stack & Conventions

- .NET 8, gRPC
- PostgreSQL + EF Core 8
- Password hashing: BCrypt.Net-Next
- Logging: structured via ILogger
- Primary constructor DI (C# 12)
- Nullable reference types enabled

## Code Patterns

- **Single consolidated `UserGrpcService`** — mapped once in API to avoid gRPC endpoint ambiguity
- **Mapper extensions**: `ToProto()` and `ToDtos()` for proto ↔ domain conversions
- **Rich domain model** with nested entities: User → DeliveryInfo (collection), Roles (many-to-many), Restrictions
- **Auto audit trail** in DbContext: `SaveChanges` override normalizes email/names and manages CreatedAt/UpdatedAt timestamps
- **Enum-based statuses**: CustomerTier (enum), Status (Active/Restricted/Banned) — not strings
- **Repository methods**: descriptive names like `ExistsByEmail()`, `CreateUser()`, `GetById()`
- **Validation in use cases**: nullability, email format, password length requirements

## Domain Model

- **UserEntity**: Id, Email, Password, Fullname, Phone, CountryCode, IsEmailVerified, CustomerTier, Status
- **DeliveryInfo**: Nested entity collection for delivery addresses
- **UserRole**: Many-to-many bridge entity
- **UserRestriction**: Tracks restricted accounts

## Configuration

- env/appsettings: PostgreSQL connection string (simpler config — no external service dependencies)
- DI registration directly in Program.cs (no module classes)

## Testing

- **Unit**: xUnit + NSubstitute
- **Integration**: xUnit + Testcontainers.PostgreSql
- Test projects mirror source layers

## Key Rules

- Credential verification (`VerifyCredentials`) is the ONLY path for password checks — never expose password hashes externally
- `GetUserByEmail` is identity lookup only (no password hash in response)
- Email normalization happens automatically on save (lowercase, trimmed)
- `IsEmailVerified` defaults to false, updated via Auth service compatibility RPC
- User service is called by Auth — ensure backward-compatible proto changes
