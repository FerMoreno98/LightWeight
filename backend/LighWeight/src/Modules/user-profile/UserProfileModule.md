# UserProfile Module

Manages the user's static personal data: name, sex and date of birth. Filled in the onboarding, right after the first login, and editable afterwards from the profile page. Other modules receive what they need through events: today the dashboard receives the sex to estimate the fat percentage.

Does **not** handle authentication, device data, time-varying metrics (weight, body fat, measurements) nor the training stage (Training has it).

## User flow

1. The user logs in with the OTP code.
2. The front asks `GET /api/user-profile`. A 404 means the profile is not completed: the user goes to `/onboarding` and cannot use the app until it is sent.
3. `POST /api/user-profile/complete` creates it. From then on the profile page (`/profile`, icon at the top right) shows it and `PUT /api/user-profile` changes it.

## Aggregate root

`User` — the user's profile: name, sex, date of birth and `StageStartedAt` (a leftover of the training stage, set at creation). Created explicitly in the onboarding, not on auth registration.

## Value objects / enums

| Type | Values | Storage |
|------|--------|---------|
| `Sex` | `Male`, `Female` | varchar(20) |

## Domain events

| Event | When | Payload |
|-------|------|---------|
| `UserCompletedDomainEvent` | `User.Create()` | `UserId`, `Sex`, `OccurredAtUtc` |
| `UserProfileModifiedDomainEvent` | `User.Modify()` | `UserId`, `Sex`, `OccurredAtUtc` |

## Integration events

| Event | Type | Handler / consumer |
|-------|------|--------------------|
| `auth.user.created.v1` | Listened | No-op: the profile is not auto-created |
| `dashboard.rebuild.requested.v1` | Listened | Publishes `userprofile.profile.recorded.v1` again |
| `userprofile.profile.recorded.v1` (`UserId`, `Sex`, `OccurredAtUtc`) | Published on complete, modify and rebuild | Dashboard (fat percentage) |

## Endpoints

All endpoints require JWT authorization. `UserId` is extracted from `ClaimTypes.NameIdentifier`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/user-profile/` | The profile `{ name, sex, dateOfBirth }`; 404 while not completed |
| POST | `/api/user-profile/complete` | Creates the profile (onboarding); 204, 409 if it already exists |
| PUT | `/api/user-profile/` | Updates name, sex and date of birth; 204 |

## Key business rules

- **Profile ≠ Auth registration**: the profile is created only when the user submits the onboarding.
- **One profile per user**: completing it twice answers 409.
- **Name up to 50 characters**, born in the last 120 years, sex `Male`/`Female` (case-insensitive).

## Database

- **Schema**: `userprofile` (PostgreSQL)
- **Tables**: `userprofile_Users` (Id, Name, Sex, DateOfBirth, StageStartedAt)
- **Migrations**: FluentMigrator in `Infrastructure/Migrations/`

## Layer structure

```
user-profile/
├── Domain/              # User, Sex, domain events, repository and unit of work interfaces
├── Application/         # CompleteProfile, UpdateProfile (+ validators), GetProfile, event handlers
├── Infrastructure/      # EF Core + FluentMigrator persistence, repository, unit of work
├── Api/                 # Minimal API endpoints + DTOs
└── testing/             # Unit tests
```
