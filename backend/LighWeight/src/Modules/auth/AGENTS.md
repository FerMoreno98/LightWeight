# AGENTS.md — Module: Auth

## 1. Module purpose

This module handles user identity and authentication via an OTP-only flow (no passwords). It issues access tokens (JWT) and refresh tokens (rotated on each use) to authorize access to other endpoints throughout the application.

## 2. Aggregates and entities

### `User` (AggregateRoot)
- **Invariants**:
  - Email must be unique across the system
  - A User can only have one active OtpCode at a time (pending OTPs are invalidated before creating a new one)
  - A DeviceToken's DeviceIdentifier must be unique per User
  - A DeviceToken can only have one active (non-revoked, non-replaced) RefreshToken at a time
- **Child entities**: `DeviceToken`, `OtpCode`, `RefreshToken`
- **Domain events it emits**: `UserCreatedDomainEvent`
- **References to other modules**: none (no external IDs)

### `DeviceToken` (Entity)
- Child of `User`
- Represents a device registered by the user
- Manages RefreshToken lifecycle (issue, validate, revoke)
- **Invariant**: `IssueRefreshToken` revokes any previously active token before issuing a new one

### `RefreshToken` (Entity)
- Child of `DeviceToken`
- 88-character base64 token, 30-day TTL
- Supports rotation: when a new token is issued from a refresh, the old one is revoked and marked with `ReplacedTokenBy`

### `OtpCode` (Entity)
- Child of `User`
- 6-digit numeric code, hashed with HMACSHA256 + salt
- Default 10-minute TTL
- One-time use only (`UsedAt` set on verification)

## 3. Non-obvious business rules

- **OTP-only auth**: There is no password or any credential other than the email OTP. Do NOT add password fields.
- **User creation is implicit**: A User is created on the fly when the first `SendOtpCode` command arrives for a new email. No registration endpoint exists.
- **Refresh token rotation**: Every time a refresh token is used to obtain a new access token, the old refresh token is revoked and a new one is issued. This detects token theft: if a revoked (and replaced) token is presented again, a `StolenRefreshTokenException` is thrown and the device's session is invalidated.
- **Email is the only identity**: The `sub` claim in the JWT is the UserId (GUID), but login is always by email. There is no username.
- **Access tokens are NOT revocable**: Only refresh tokens can be revoked. Access tokens are short-lived (configured via `Jwt:AccessTokenExpirationMinutes`) and left to expire.
- **JWT secret is HMAC-SHA256**: The JWT is symmetric, signed with `Jwt:Secret`. DO NOT change to asymmetric unless you update all consuming services.
- **`DeviceTokenAddedDomainEvent` is defined but NOT yet raised** in the current code. Do not rely on it being dispatched.

## 4. Folder structure

```
auth/
├── LightWeight.Auth.Domain/
│   ├── Aggregates/            # User (the only aggregate root)
│   ├── Entities/              # OtpCode, DeviceToken, RefreshToken
│   ├── Events/                # UserCreatedDomainEvent, DeviceTokenAddedDomainEvent
│   ├── Repository/            # IUserRepository interface
│   ├── Uow/                   # IAuthUnitOfWork interface
│   ├── Services/              # ICodeHasher, IEmailSender, IJwtTokenGenerator interfaces
│   └── Exceptions/            # Domain exceptions (per entity)
│
├── LightWeight.Auth.Application/
│   ├── Commands/
│   │   ├── LoginOtp/
│   │   │   ├── SendOtpCode/           # Command + Handler
│   │   │   └── VerifyOtpCode/         # Command + Handler + OtpLoginResult record
│   │   ├── LoginWithRefreshToken/     # Command + Handler + LoginRefreshTokenResult
│   │   └── Logout/                    # Command + Handler
│   ├── Events/                        # UserCreatedDomainEventHandler (domain → integration event)
│   └── Exceptions/                    # Application-level exceptions
│
├── LightWeight.Auth.Infrastructure/
│   ├── Persistence/
│   │   ├── AuthDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   └── Repositories/
│   │       └── UserRepository.cs
│   ├── Configurations/           # EF Core IEntityTypeConfiguration per entity
│   ├── Migrations/               # FluentMigrator migrations (schema "auth")
│   └── Services/                 # TokenProvider, CodeHasher, EmailSender
│
├── LightWeight.Auth.Api/
│   ├── DTOs/                     # Request/response records
│   └── AuthModule.cs             # Endpoint mapping (MapAuthEndpoints)
│
└── testing/
    └── LightWeight.Auth.UnitTests/
        ├── Domain/               # Unit tests per entity
        └── Application/Commands/ # Handler tests per command
```

## 5. Database schema

- **Schema**: `auth` (PostgreSQL, managed by FluentMigrator and EF Core)
- **Tables**:
  | Table | Columns | FK |
  |-------|---------|----|
  | `auth_Users` | `Id` (guid PK), `Email` (varchar 200, unique) | -- |
  | `auth_DeviceTokens` | `Id` (guid PK), `UserId`, `DeviceIdentifier`, `DeviceName` (200), `Platform` (50), `LastSeenAt`, `CreatedAt`, `RevokedAt?` | `UserId` → `auth_Users.Id` (cascade). Unique index on `(UserId, DeviceIdentifier)`. |
  | `auth_RefreshTokens` | `Id` (guid PK), `DeviceTokenId`, `Token` (varchar 88, unique), `ExpiresAt`, `RevokedAt?`, `ReplacedTokenBy?` (88), `CreatedByIp` (45) | `DeviceTokenId` → `auth_DeviceTokens.Id` (cascade). |
  | `auth_OtpCodes` | `Id` (guid PK), `UserId`, `Hash` (255), `CreatedAt`, `ExpiresAt`, `UsedAt?` | `UserId` → `auth_Users.Id` (cascade). Index on `UserId`. |
- **Migrations**: located in `Infrastructure/Persistence/Migrations/`, naming convention `{yyyyMMddHHmm}_{Description}.cs`

## 6. Endpoints exposed

| Method | Route | Command | Auth | Notes |
|--------|-------|---------|------|-------|
| POST | `/api/auth/otp/send` | `SendOtpCodeCommand` | Anonymous | Generates 6-digit OTP and emails it; creates User on first visit |
| POST | `/api/auth/otp/verify` | `VerifyOtpCodeCommand` | Anonymous | Verifies OTP hash, registers device, issues access+refresh tokens |
| POST | `/api/auth/refresh` | `LoginWithRefreshTokenCommand` | Anonymous | Rotates refresh token, returns new token pair |
| POST | `/api/auth/logout` | `LogoutCommand` | **Authorized** | Revokes the specific refresh token |

## 7. Dependencies

- **Depends on**: `LightWeight.Shared` (AggregateRoot, Entity, IDomainEvent, IMediator, ICommand, IEventPublisher, etc.)
- **Depended by**: `UserProfile` module (consumes `UserCreatedIntegrationEvent`)
- **Integration events it publishes**: `UserCreatedIntegrationEvent` (event name: `"auth.user.created.v1"`)
- **Integration events it listens to**: None

## 8. Current state

**Done:**
- User aggregate with OTP-only registration/login
- Device management with refresh token rotation
- Stolen token detection
- Full unit test coverage for all 4 commands and all 4 domain entities
- FluentMigrator migrations (4 tables in `auth` schema)
- EF Core persistence with domain event dispatch via UnitOfWork
- JWT token generation (HMAC-SHA256, configurable TTL)
- Email sending via SMTP (MailHog at localhost:1025)

**Pending / in progress:**
- `DeviceTokenAddedDomainEvent` is defined but not yet raised — its handler may need to be implemented when cross-module concerns arise

**Decisions an agent must NOT revert without asking:**
- OTP-only auth: no passwords, no external OAuth providers have been introduced
- Refresh token rotation (revoke+replace on use)
- Access tokens are short-lived and NOT revocable
- Schema name is `auth`, all tables prefixed with `auth_`

## 9. Tests

- **Location**: `testing/LightWeight.Auth.UnitTests/`
- **What to cover when touching this module**: invariants of all 4 domain entities, all 4 command handlers (happy path + error cases), edge cases (expired/used/stolen tokens, duplicate devices)
- **Mocking pattern**: NSubstitute — assign to a local variable before chaining `.Returns()`. Use `FluentAssertions` for assertions.

## 10. Things NOT to do in this module

- Do NOT add password fields, password hashing, or any credential other than email OTP
- Do NOT add business logic in the Endpoint (Api layer) — only map to Command/Query and send via `IMediator`
- Do NOT create EF Core relationships in more than one `IEntityTypeConfiguration` for the same FK
- Do NOT add queries to this module without considering whether they belong in a dedicated read-side module (this module has no queries yet)
- Do NOT assume User exists before `SendOtpCode` — user creation is implicit on first OTP request
- Do NOT change JWT algorithm from HMAC-SHA256 without updating all consumers
- Do NOT add dependencies on other module's domain types — communicate only via integration events
