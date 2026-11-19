# Module: Nutrition

## Responsibility

Keeps a copy of the **daily totals of the user's food diary** (calories and macronutrients) so the Dashboard can relate them to training performance. **FatSecret is the user's nutrition app** and the only source: LightWeight does not log food, edit days nor store goals. Decided on 2026-11-19.

What it does **not** do (on purpose):
- No manual logging: the user logs in FatSecret, LightWeight only reads.
- No meal or food breakdown: only the totals of the day (see *Why daily totals*).
- No calorie/macro goals: the FatSecret API does not return them (only `goal_weight_kg` in `profile.get`), and asking the user to type them again here goes against FatSecret being the nutrition app.
- No analytics: averages, g/kg and the relation with performance are calculated by the Dashboard.

## Aggregate: `DiaryDay`

The diary of a user for one calendar day.

| Property | Type | Notes |
|---|---|---|
| `UserId` | Guid | Owner |
| `Date` | DateOnly | Calendar day of the user in **Europe/Madrid** (fixed for now). Unique per user |
| `Intake` | `DailyIntake` | `Calories` (kcal), `Protein`, `Carbohydrate`, `Fat` (g) |

- **`DailyIntake`** (value object): every value ≥ 0. The calories are **not** checked against the macros (4/4/9): alcohol and rounding make them differ.
- **`Create(userId, date, intake, today)`**: `date ≤ today` (the user's day, Europe/Madrid). Raises `DiaryDayRecordedDomainEvent`.
- **`Update(intake)`**: replaces the totals; **no event if they are the same**, so a synchronization that changes nothing publishes nothing.
- **`Delete()`**: raises `DiaryDayDeletedDomainEvent`; the repository removes the row when saving (same two steps as BodyMetrics).

## Synchronization: `DiarySynchronization` (domain service)

`Apply(userId, period, recorded, synced, today)` brings the recorded days of a `DiaryPeriod` (from–to, both included) in line with what FatSecret returned. **FatSecret is the source of truth**:

| Day in the period | Recorded here | Came from FatSecret | Result |
|---|---|---|---|
| | no | yes | created |
| | yes | yes | updated (event only if it changed) |
| | yes | no | **deleted**: FatSecret omits the days without entries, so the user emptied it |

It rejects synced or recorded days out of the period, repeated days and days of another user. The data arrives **once a night** (from the 1st of the previous month to today) plus a full backfill when the account is linked; see `deviceintegration/DeviceIntegration.md`. So the Dashboard shows the diary up to the previous night: today is never in it until the next run. Knowing the period is what lets an absent day mean "deleted" — that is why the event from DeviceIntegration carries it.

## Why daily totals

FatSecret offers three levels: totals per day (`food_entries.get_month`: one call per month), per meal and per food (`food_entries.get`: one call per day). For performance what matters is whether the user arrived at the session with energy: kcal and carbohydrate of the previous day and the same day, average protein of the week. The meal level would allow "carbs at lunch before an afternoon session", but FatSecret only says the meal category, **not the time**, so that relation would be unreliable. A `Meals` collection can be added to `DiaryDay` later without redoing anything.

## Dates and time zone

FatSecret's `date_int` is a calendar day (days since 1970-01-01) with no time zone. Sessions are stored in UTC (`StartedAt`). The day of a session is its `StartedAt` converted to **Europe/Madrid** — the same zone used to compute `today`. There is no user time zone in the project yet; if one is added (UserProfile), it replaces the fixed zone.

## Events Emitted

| Integration event | Payload | When |
|---|---|---|
| `nutrition.day.recorded.v1` | `DiaryDayId`, `UserId`, `Date`, `Calories`, `Protein`, `Carbohydrate`, `Fat` | A day is created or its totals change, and for every day during a rebuild |
| `nutrition.day.deleted.v1` | `DiaryDayId`, `UserId`, `Date` | A recorded day no longer has entries in FatSecret |

Consumers upsert by `DiaryDayId` (same pattern as BodyMetrics). Contracts: `shared/Contracts/Nutrition/NutritionIntegrationEvents.cs`; the synced event, `shared/Contracts/DeviceIntegration/DeviceIntegrationIntegrationEvents.cs`.

## Events Subscribed To

| Integration event | Origin | Action |
|---|---|---|
| `deviceintegration.fatsecret.diary.synced.v1` (`UserId`, `From`, `To`, `Days[{ Date, Calories, Protein, Carbohydrate, Fat }]`) | DeviceIntegration | Load the recorded days of the period, `DiarySynchronization.Apply`, save |
| `dashboard.rebuild.requested.v1` | Dashboard | Publish `nutrition.day.recorded.v1` for every day of the user (read-only) |

## Use in the Dashboard (planned)

A `dashboard_NutritionDays` projection. Calculated when reading: kcal and carbohydrate (g/kg) of the previous day and the same day of each session, 7-day average protein (g/kg) and kcal; the body weight for g/kg is the weekly average the Dashboard already has. Shown as extra rows under sleep/DOMS/motivation in the session performance chart.

## Implementation notes

- **Handlers** (`Application/Events`): `FatSecretDiarySyncedIntegrationEventHandler` (loads the period, `DiarySynchronization.Apply`, adds/removes, saves), `RebuildRequestedIntegrationEventHandler` (read-only, `AsNoTracking`), `DiaryDayDomainEventsHandler` (domain → integration events, built in `NutritionIntegrationEventMapper`). No commands, queries nor endpoints: the data only arrives from DeviceIntegration and is read by the Dashboard. `LightWeight.Nutrition.Api` stays empty until an endpoint is needed.
- **Today** comes from `Application/Utils/UserCalendar.Today(IClock.UtcNow)` (Europe/Madrid, via `TimeZoneInfo`; the Docker runtime image, Ubuntu 24.04, has the tz data).
- **A failing synchronization saves nothing** (an invalid day throws before `SaveChanges`); `EventPublisher` logs it and the next night tries again.
- **Schema** `nutrition`, table `nutrition_DiaryDays`: `Id` uuid PK, `UserId`, `Date` date, `Calories` decimal(7,2), `Protein`/`Carbohydrate`/`Fat` decimal(6,2); unique index `Ux_DiaryDays_UserId_Date`. `Intake` is a `ComplexProperty` (same row). Migrations `2026111910xx` (shared `VersionInfo`: keep using this range).

## State

**Done (2026-11-19):** domain, integration contracts, Application handlers, Infrastructure (schema, migrations, repository, UnitOfWork with event dispatch), host registration (`AddNutritionModule`), unit tests for domain and application; checked against Postgres (create, no-op, update, delete, rebuild).

**Pending:** the FatSecret connection and nightly run in DeviceIntegration, and the Dashboard projection and charts.

## Ideas for later

- Meal breakdown (`food_entries.get`) if a use for it appears.
- Supplements: list, mark the active ones with dose and frequency, unmark when stopped (keeping when each was taken).
