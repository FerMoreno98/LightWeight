# AGENTS.md — Module: UserProfile

## 1. Module purpose

This module manages the user's static personal data (name, sex, date of birth). It is filled in the onboarding, right after the first login, and other modules use it (today the dashboard uses the sex for the fat percentage). It does **not** handle authentication, device data, time-varying metrics nor the training stage (that one belongs to Training: the macrocycle has it).

## 2. Aggregates and entities

### `User` (AggregateRoot)
- **Invariants** (checked by the validators of the commands, the aggregate has no checks of its own):
  - One profile per UserId: `CompleteProfile` throws `UserProfileAlreadyCompletedException` (409) if it exists
  - `Name` is required and max 50 characters (the column is varchar(50))
  - `DateOfBirth` must be in the past and not more than 120 years ago
  - `Sex` must be `Male` or `Female`
- `StageStartedAt`: a leftover of the training stage, set at creation and never updated. It is still mapped and stored; it can be dropped with a migration if nobody needs it.
- **Domain events**: `UserCompletedDomainEvent(UserId, Sex, OccurredAtUtc)` on `Create()`, `UserProfileModifiedDomainEvent(UserId, Sex, OccurredAtUtc)` on every `Modify()`.
- **References to other modules**: None (only its own `UserId`, which comes from the Auth module's JWT)

## 3. Non-obvious business rules

- **Profile is independent of Auth registration**: the `UserCreatedIntegrationEvent` handler is a no-op. The profile is created only when the user submits the onboarding (`POST /complete`).
- **The front decides the onboarding with `GET /api/user-profile`**: 404 means "not completed yet". The guards of the front (`Modules/UserProfile/profile.guards.ts`) send the user to `/onboarding` until it is completed.
- **Published as a "Recorded" event**: both domain events become `ProfileRecordedIntegrationEvent(UserId, Sex, OccurredAtUtc)` (`userprofile.profile.recorded.v1`), with the current value, so consumers just upsert. `Modify` raises it even if the sex did not change: repeating it is harmless.
- **Answers the dashboard rebuild**: `RebuildRequestedIntegrationEventHandler` publishes the profile again (nothing if the user has no profile).
- **Enums stored as strings**: `Sex` is varchar(20). Parsing is case-insensitive.
- **UserId comes from the JWT claim `ClaimTypes.NameIdentifier`**. The request DTOs do not carry it.
- **Dates**: the front sends the date of birth as `YYYY-MM-DD`; the endpoint marks it as UTC, so it is stored as midnight UTC and the front reads the date part back.

## 4. Folder structure

```
user-profile/
├── LightWeight.UserProfile.Domain/
│   ├── Aggregates/         # User
│   ├── Enum/               # Sex
│   ├── Events/             # UserCompletedDomainEvent, UserProfileModifiedDomainEvent
│   ├── Repository/         # IUserRepository
│   └── Uow/                # IUserProfileUnitOfWork
│
├── LightWeight.UserProfile.Application/
│   ├── Commands/
│   │   ├── CompleteProfile/        # Command + Handler + Validator
│   │   └── UpdateProfile/          # Command + Handler + Validator
│   ├── Queries/GetProfile/         # Query + Handler (404 without profile)
│   ├── Events/                     # UserProfileDomainEventsHandler (publishes), RebuildRequested handler, UserCreated (no-op)
│   └── Exceptions/                 # UserNotFoundException (404), UserProfileAlreadyCompletedException (409)
│
├── LightWeight.UserProfile.Infrastructure/
│   ├── Persistence/        # UserProfileDbContext, UnitOfWork, Repositories/UserRepository
│   ├── Configurations/     # EF Core configuration of User
│   └── Migrations/         # FluentMigrator (schema "userprofile")
│
├── LightWeight.UserProfile.Api/
│   ├── DTOs/                # CompleteProfileRequest, UpdateProfileRequest
│   └── UserProfileModule.cs # MapUserProfileEndpoints
│
└── testing/
    └── LightWeight.UserProfile.UnitTests/
        ├── Domain/           # User
        └── Application/      # handlers, query and events
```

## 5. Database schema

- **Schema**: `userprofile` (PostgreSQL, created by FluentMigrator, mapped by EF Core)
- **Tables**:
  | Table | Columns | FK |
  |-------|---------|----|
  | `userprofile_Users` | `Id` (uuid PK), `Name` (varchar 50), `Sex` (varchar 20), `DateOfBirth` (timestamp), `StageStartedAt` (timestamp) | None |
- **Migrations**: `Infrastructure/Migrations/{yyyyMMddHHmm}_{Description}.cs`. `202611101000_DropCurrentStageFromUsers` removed the old `CurrentStage` column, which was NOT NULL and no longer mapped, so creating a profile failed. Every module shares the `VersionInfo` table: never reuse a version of another module.

## 6. Endpoints exposed

All endpoints require JWT authorization.

| Method | Route | Handler | Answer |
|--------|-------|---------|--------|
| GET | `/api/user-profile/` | `GetProfileQuery` | `{ name, sex, dateOfBirth }`; **404** while the onboarding is not done |
| POST | `/api/user-profile/complete` | `CompleteProfileCommand` | Body `{ fullName, birthDate, sex }`; 204; **409** if it already exists |
| PUT | `/api/user-profile/` | `UpdateProfileCommand` | Body `{ fullName, dateOfBirth, sex }`; 204; 404 without profile |

## 7. Dependencies

- **Depends on**: `LightWeight.Shared` (building blocks, mediator, messaging, contracts).
- **Integration events it listens to**: `auth.user.created.v1` (no-op), `dashboard.rebuild.requested.v1` (publishes the profile again).
- **Integration events it publishes**: `userprofile.profile.recorded.v1` (consumed by the dashboard).
- **Shared services** (`IMediator`, `IEventPublisher`, validation pipeline) are registered with `TryAdd`/existence checks.

## 8. Current state

**Done:** onboarding and edition (backend and front), profile query, sex published to the dashboard, unit tests.

**Front:** `frontend/LightWeight/src/app/Modules/UserProfile` — `data/user-profile-api.service.ts`, `state/user-profile.store.ts` (loads the profile once per session; `reset()` on login), `profile.guards.ts` (`profileCompletedGuard` on every page of the app, `onboardingGuard` on `/onboarding`), `UI/profile-form` (shared by both pages), `UI/complete-profile-page` (`/onboarding`, without navbar) and `UI/profile-page` (`/profile`, icon at the top right of the navbar).

**Pending:**
- `UserCreatedIntegrationEventHandler` is a no-op.
- `StageStartedAt` has no use since the stage moved to Training.

**Decisions an agent must NOT revert without asking:**
- Profile creation is decoupled from Auth registration (explicit onboarding, not automatic)
- Enums are persisted as strings (`varchar(20)`), not integers
- Schema name is `userprofile`, table prefixed with `userprofile_`

## 9. Tests

- **Location**: `testing/LightWeight.UserProfile.UnitTests/`
- **What to cover when touching this module**: `User` creation and modification (and their events), the handlers (happy path, not found, already completed), the query, the published events and the rebuild, enum parsing (case-insensitive), cancellation token propagation.
- **Mocking pattern**: NSubstitute — assign to a local variable before chaining `.Returns()`. xUnit assertions.

## 10. Things NOT to do in this module

- Do NOT add authentication or device-management logic (belongs to Auth)
- Do NOT add time-varying metrics (weight, body fat... belong to BodyMetrics) nor the training stage (Training)
- Do NOT assume a profile exists just because the user is authenticated
- Do NOT store enums as integers without updating all queries and consumers
- Do NOT add business logic in the Endpoint layer — only map to Command/Query and send via `IMediator`
- Do NOT depend on other modules' types — communicate only via integration events
