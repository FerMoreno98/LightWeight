# UserProfile Module

Manages the user's static physical and personal data (name, sex, date of birth, training stage). Populated during onboarding, right after first authentication. Serves as foundation for calculations in BodyMetrics, Nutrition, and Training modules.

Does **not** handle authentication, device data, or time-varying metrics (weight, body fat, measurements).

## Aggregate root

`User` — the user's profile. Contains name, sex, date of birth, current training stage, and when the stage started. Created explicitly when the user completes onboarding (not automatically on auth registration).

## Value objects / enums

| Type | Values | Storage |
|------|--------|---------|
| `Sex` | `Male`, `Female` | varchar(20) |
| `TrainingStage` | `Bulk`, `Cut`, `Maintenance` | varchar(20) |

## Domain events

| Event | When | Payload | Status |
|-------|------|---------|--------|
| `UserCompletedDomainEvent` | `User.Create()` | `UserId`, `OccurredAtUtc` | Implemented |
| `UserProfileUpdated` | `User.Modify()` | — | **Not implemented** |

## Integration events

| Event | Type | Handler |
|-------|------|---------|
| `UserCreatedIntegrationEvent` (from Auth) | Listened | **No-op** — profile is not auto-created |
| `UserCompletedDomainEvent` | Published | Consumed by BodyMetrics, Nutrition, Training |

## Endpoints

All endpoints require JWT authorization. `UserId` is extracted from `ClaimTypes.NameIdentifier`.

| Method | Route | Command | Description |
|--------|-------|---------|-------------|
| POST | `/api/user-profile/complete` | `CompleteProfileCommand` | Creates profile on onboarding |
| PUT | `/api/user-profile/` | `UpdateProfileCommand` | Updates name/sex/date of birth |
| PATCH | `/api/user-profile/training-stage` | `ChangeTrainingStageCommand` | Changes training stage (Bulk/Cut/Maintenance) |

All return `204 No Content`.

## Key business rules

- **Profile ≠ Auth registration**: the profile is created only when the user submits the onboarding form. The `UserCreatedIntegrationEvent` handler is a no-op.
- **Enums as strings**: `Sex` and `TrainingStage` are stored as varchar, not integers. Parsing is case-insensitive.
- **`StageStartedAt` is static**: set once at profile creation, never updated on stage changes.
- **No `UserProfileUpdated` event yet**: `User.Modify()` does not raise a domain event. If cross-module notifications are needed, implement it.

## Database

- **Schema**: `userprofile` (PostgreSQL)
- **Tables**: `userprofile_Users` (Id, Name, Sex, DateOfBirth, CurrentStage, StageStartedAt)
- **Migrations**: FluentMigrator in `Infrastructure/Persistence/Migrations/`

## Dependencies

- **Depends on**: `LightWeight.Shared` (building blocks). Listens to `UserCreatedIntegrationEvent` from Auth.
- **Depended by**: BodyMetrics, Nutrition, Training (consume `UserCompletedDomainEvent`)

## Layer structure

```
user-profile/
├── Domain/              # Aggregate (User), Enums, Events, Repository interface, Unit of Work interface
├── Application/         # Commands (CompleteProfile, UpdateProfile, ChangeTrainingStage) + Validators, Event handler
├── Infrastructure/      # EF Core + FluentMigrator persistence, Repository, UnitOfWork
├── Api/                 # Minimal API endpoints + DTOs
└── testing/             # Unit tests for domain entity and all command handlers
```
