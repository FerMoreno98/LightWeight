# AGENTS.md — Module: UserProfile

## 1. Module purpose

This module manages the user's static physical and personal data (name, sex, date of birth, training stage). It is populated during onboarding, right after first authentication, and serves as a foundation for other modules (basal metabolic rate, body fat, calorie targets, etc.). It does **not** handle authentication, device data, or time-varying metrics.

## 2. Aggregates and entities

### `User` (AggregateRoot)
- **Invariants**:
  - A User profile can only be created once per UserId (no duplicate profiles)
  - `Name` is required and max 50 characters
  - `DateOfBirth` must be in the past and not more than ~120 years ago
  - `Sex` must be a valid value (`Male` or `Female`)
  - `CurrentStage` must be a valid value (`Bulk`, `Cut`, or `Maintenance`)
  - `StageStartedAt` is set to the creation date and never updated automatically on stage changes
- **Child entities**: None (no child entities; all properties are value objects/enums)
- **Domain events it emits**: `UserCompletedDomainEvent` (raised on `Create()`)
- **References to other modules**: None (only references its own `UserId`, which comes from the Auth module's JWT)

## 3. Non-obvious business rules

- **Profile is independent of Auth registration**: The `UserCreatedIntegrationEvent` from Auth is listened to but currently ignored (no-op handler). The profile is created only when the user explicitly completes onboarding via the `CompleteProfile` endpoint.
- **No `UserProfileUpdated` event yet**: `User.Modify()` does NOT raise a domain event. The module documentation mentions it should, but this is not yet implemented. Do not rely on it being dispatched.
- **Enums stored as strings**: `Sex` and `TrainingStage` are stored as varchar(20) in the database, not as integers. Validators are case-insensitive when parsing.
- **Training stage has no start-date update on change**: `StageStartedAt` is only set at creation. Changing the stage via `ChangeStage` does NOT update `StageStartedAt`. If time-tracking per stage is needed, that logic must be explicitly added.
- **UserId comes from the JWT claim `ClaimTypes.NameIdentifier`**: All endpoints extract the user's GUID from the authenticated token. The DTO also includes a `UserId` field, but the handler uses the one from the claim.

## 4. Folder structure

```
user-profile/
├── LightWeight.UserProfile.Domain/
│   ├── Aggregates/         # User
│   ├── Enum/               # Sex, TrainingStage
│   ├── Events/             # UserCompletedDomainEvent
│   ├── Repository/         # IUserRepository interface
│   └── Uow/                # IUserProfileUnitOfWork interface
│
├── LightWeight.UserProfile.Application/
│   ├── Commands/
│   │   ├── CompleteProfile/        # Command + Handler + Validator
│   │   ├── UpdateProfile/          # Command + Handler + Validator
│   │   └── ChangeTrainingStage/    # Command + Handler + Validator
│   ├── Events/                     # UserCreatedIntegrationEventHandler (no-op)
│   └── Exceptions/                 # UserNotFoundException
│
├── LightWeight.UserProfile.Infrastructure/
│   ├── Persistence/
│   │   ├── UserProfileDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   └── Repositories/
│   │       └── UserRepository.cs
│   ├── Configurations/      # EF Core IEntityTypeConfiguration for User
│   └── Migrations/          # FluentMigrator migration (schema "userprofile")
│
├── LightWeight.UserProfile.Api/
│   ├── DTOs/                # Request records
│   └── UserProfileModule.cs # Endpoint mapping (MapUserProfileEndpoints)
│
└── testing/
    └── LightWeight.UserProfile.UnitTests/
        ├── Domain/           # User entity tests
        └── Application/      # Handler tests per command
```

## 5. Database schema

- **Schema**: `userprofile` (PostgreSQL, managed by FluentMigrator and EF Core)
- **Tables**:
  | Table | Columns | FK |
  |-------|---------|----|
  | `userprofile_Users` | `Id` (guid PK), `Name` (varchar 50), `Sex` (varchar 20), `DateOfBirth` (timestamp), `CurrentStage` (varchar 20), `StageStartedAt` (timestamp) | None |
- **Migrations**: located in `Infrastructure/Persistence/Migrations/`, naming convention `{yyyyMMddHHmm}_{Description}.cs`

## 6. Endpoints exposed

All endpoints require JWT authorization. `UserId` is extracted from `ClaimTypes.NameIdentifier` in the token.

| Method | Route | Command | Notes |
|--------|-------|---------|-------|
| POST | `/api/user-profile/complete` | `CompleteProfileCommand` | Creates profile on onboarding; returns 204 |
| PUT | `/api/user-profile/` | `UpdateProfileCommand` | Updates name/sex/date of birth; returns 204 |
| PATCH | `/api/user-profile/training-stage` | `ChangeTrainingStageCommand` | Changes training stage (Bulk/Cut/Maintenance); returns 204 |

## 7. Dependencies

- **Depends on**: `LightWeight.Shared` (AggregateRoot, Entity, IDomainEvent, IMediator, ICommand, etc.). Listens to `UserCreatedIntegrationEvent` from Auth module.
- **Depended by**: BodyMetrics, Nutrition, and Training modules (consume `UserCompletedDomainEvent`)
- **Integration events it listens to**: `UserCreatedIntegrationEvent` from Auth (event name: `"auth.user.created.v1"`) — handler is currently no-op
- **Integration events it publishes**: `UserCompletedDomainEvent` (dispatched after profile creation). `UserProfileUpdated` is mentioned in docs but NOT yet implemented.

## 8. Current state

**Done:**
- Single `User` aggregate with creation, update, and training-stage-change operations
- FluentValidation validators for all commands
- EF Core persistence with domain event dispatch via UnitOfWork
- FluentMigrator migration (1 table in `userprofile` schema)
- Full unit test coverage for domain entity and all 3 command handlers
- All endpoints require JWT auth and extract `UserId` from claims

**Pending / in progress:**
- `UserProfileUpdated` domain event is documented but NOT raised by `User.Modify()` — implement when cross-module notifications are needed
- `UserCreatedIntegrationEventHandler` is a no-op — the profile is created only via onboarding, not automatically on Auth registration

**Decisions an agent must NOT revert without asking:**
- Profile creation is decoupled from Auth registration (explicit onboarding, not automatic)
- Enums are persisted as strings (`varchar(20)`), not integers
- `StageStartedAt` is only set once on creation and never updated on stage changes
- Schema name is `userprofile`, table prefixed with `userprofile_`

## 9. Tests

- **Location**: `testing/LightWeight.UserProfile.UnitTests/`
- **What to cover when touching this module**: `User` aggregate invariants (creation, modification, stage change), all 3 command handlers (happy path + not-found case), enum parsing (case-insensitivity), cancellation token propagation
- **Mocking pattern**: NSubstitute — assign to a local variable before chaining `.Returns()`. Use xUnit assertions (no FluentAssertions dependency found in the test project).

## 10. Things NOT to do in this module

- Do NOT add authentication or device-management logic (belongs to Auth module)
- Do NOT add time-varying metrics (weight, body fat, etc. — belongs to BodyMetrics module)
- Do NOT assume a User profile exists just because the user is authenticated — profile is created separately during onboarding
- Do NOT store enums as integers without updating all queries and consumers
- Do NOT add business logic in the Endpoint layer — only map to Command and send via `IMediator`
- Do NOT add dependencies on other module's domain types — communicate only via integration/domain events
- Do NOT change the `StageStartedAt` behavior without confirming the intended semantics
