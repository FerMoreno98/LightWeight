# Auth Module

Manages user identity and authentication via an **OTP-only flow** (no passwords). Issues JWT access tokens and refresh tokens (rotated on each use) to authorize access across the application.

## Aggregate root

`User` — minimal identity for authentication. Contains email, device tokens, OTP codes, and refresh tokens. Created implicitly on the first `SendOtpCode` request (no registration endpoint).

## Entities

| Entity | Parent | Purpose |
|--------|--------|---------|
| `OtpCode` | User | 6-digit code, hashed (HMACSHA256 + salt), 10-min TTL, one-time use |
| `DeviceToken` | User | Registered device (identifier, name, platform). Manages refresh token lifecycle |
| `RefreshToken` | DeviceToken | 88-char base64 token, 30-day TTL. Supports rotation on each use |

## Domain events

| Event | When | Payload |
|-------|------|---------|
| `UserCreatedDomainEvent` | `User.Create()` | `UserId`, `Email`, `OccurredAtUtc` |

## Integration events

| Event | When | Consumers |
|-------|------|-----------|
| `UserCreatedIntegrationEvent` | After domain event dispatch | UserProfile (no-op) |

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/otp/send` | Anonymous | Sends 6-digit OTP via email; creates User on first visit |
| POST | `/api/auth/otp/verify` | Anonymous | Verifies OTP, registers device, returns access + refresh tokens |
| POST | `/api/auth/refresh` | Anonymous | Rotates refresh token, returns new token pair |
| POST | `/api/auth/logout` | Authorized | Revokes the specific refresh token |

## Key business rules

- **OTP-only**: no passwords, no OAuth. All authentication flows through email OTP.
- **Implicit user creation**: sending an OTP to a new email auto-creates the user.
- **Refresh token rotation**: using a refresh token revokes the old one and issues a new one. A revoked+replaced token presented again triggers `StolenRefreshTokenException` and invalidates the device session.
- **Access tokens are NOT revocable**: they are short-lived (configurable via `Jwt:AccessTokenExpirationMinutes`) and left to expire.
- **JWT**: HMAC-SHA256 signed. Claims: `sub` (UserId), `email`.

## Database

- **Schema**: `auth` (PostgreSQL)
- **Tables**: `auth_Users`, `auth_DeviceTokens`, `auth_RefreshTokens`, `auth_OtpCodes`
- **Migrations**: FluentMigrator in `Infrastructure/Persistence/Migrations/`

## Dependencies

- **Depends on**: `LightWeight.Shared` (building blocks)
- **Depended by**: UserProfile (consumes `UserCreatedIntegrationEvent`)

## Layer structure

```
auth/
├── Domain/              # Aggregates, Entities, Events, Repository interfaces, Service interfaces, Exceptions
├── Application/         # Commands (SendOtpCode, VerifyOtpCode, LoginWithRefreshToken, Logout), Event handlers
├── Infrastructure/      # EF Core + FluentMigrator persistence, TokenProvider, CodeHasher, EmailSender
├── Api/                 # Minimal API endpoints + DTOs
└── testing/             # Unit tests for all entities and command handlers
```
