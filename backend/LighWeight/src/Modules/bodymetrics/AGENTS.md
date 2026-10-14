# AGENTS.md — Module: BodyMetrics

## 1. Module purpose

Registers the body measurements of the user — **body weight** (kg), **perimeters** (cm) and **skinfolds** (mm) — and lets them be corrected and deleted. It publishes every change as an integration event so the Dashboard can build the evolution charts. The history endpoints only return the latest 10 measurements of each kind: analysing the whole history is the Dashboard's job, not this module's. See `BodyMetrics.md` for the functional description and the event contracts.

## 2. Aggregates and value objects

### `WeightMeasurement` (AggregateRoot)
- **Properties**: `UserId`, `Weight` (decimal, kg), `MeasuredAt` (UTC)
- **Invariants**: `MinWeight = 20 ≤ Weight ≤ MaxWeight = 400`; `UserId` not empty; `MeasuredAt` not in the future
- **Methods**: `Create(userId, weight, measuredAt, now)`, `Correct(weight, measuredAt, now)`, `Delete()`
- **DB table**: `bodymetrics_WeightMeasurements`

### `Perimeters` (AggregateRoot)
- **Properties**: `UserId`, `Measures` (`PerimeterMeasures`), `MeasuredAt`
- **Methods**: `Create(userId, PerimeterMeasures, measuredAt, now)`, `Correct(PerimeterMeasures, measuredAt, now)`, `Delete()`
- **DB table**: `bodymetrics_Perimeters` (one nullable column per perimeter)

### `SkinFolds` (AggregateRoot)
- **Properties**: `UserId`, `Measures` (`SkinFoldMeasures`), `MeasuredAt`
- **Methods**: `Create`, `Correct`, `Delete` (as above) + `CalculateEstimatedFatPercentage(Sex)` (`0.1051·Σ + 2.585` men, `0.1548·Σ + 3.580` women; null without the six skinfolds)
- **DB table**: `bodymetrics_SkinFolds` (one nullable column per skinfold; the summatory is not stored)

### Value objects (`Domain/ValueObjects`)
- `PerimeterMeasures` (14 `decimal?`) and `SkinFoldMeasures` (6 `decimal?` + calculated `Summatory`) are the set of measures taken together. They validate themselves in `Create(...)` (named arguments: `PerimeterMeasures.Create(waist: 82m)`), so **an instance is always valid**: at least one measure and each one in range (`0 < x ≤ 300` cm / `0 < x ≤ 100` mm).
- `MeasurementRules` (internal) holds the shared rules: range check, `EnsureNotInTheFuture` (5 min clock tolerance) and `EnsureUser`.

## 3. Non-obvious business rules

- **Correct replaces the whole measurement.** `Correct` receives a new `PerimeterMeasures`/`SkinFoldMeasures`: a perimeter not sent in a correction becomes null. The API mirrors it (`PUT` with the full body).
- **Delete is two steps.** `aggregate.Delete()` raises the `…DeletedDomainEvent`, then `repository.Remove(aggregate)` deletes the row when saving. Always call both, in that order, or the event is lost.
- **The UnitOfWork collects the aggregates before saving** (`Persistence/UnitOfWork.cs`). EF Core detaches deleted entities in `SaveChanges`; collecting them first is what lets the deleted events be dispatched. Keep that order if the UnitOfWork is touched.
- **Register and correct publish the same integration event** (`…Recorded`, full values). Consumers upsert by `MeasurementId`; `…Deleted` removes it.
- **Domain events carry the real time of the event** (`DomainEvent.OccurredAtUtc`, set on creation); the date of the measurement goes in its own `MeasuredAt` field. Do not mix them.
- **Dates are UTC.** Columns are `timestamp with time zone`. The application normalizes the incoming date (`Application/Utils/UtcDate.Normalize`): a date without zone is taken as UTC, a local one is converted. Npgsql rejects non-UTC `DateTime` for these columns.
- **Time comes from `IClock`**, never `DateTime.UtcNow`, so handlers can be tested with a fixed time.
- **History = latest 10.** `Get…HistoryQuery.Count` is the only place where the number lives. Queries are not paginated on purpose (see `BodyMetrics.md`).
- **The shared mediator does not validate queries** — only commands go through `ValidationPipelineBehavior`. A FluentValidation validator for a query is never executed: check query input in the handler.
- **Name clash:** the namespaces `Commands.Perimeters` and `Commands.SkinFolds` hide the aggregates of the same name. The handlers use an alias (`using PerimetersAggregate = LightWeight.BodyMetrics.Domain.Aggregates.Perimeters;`).

## 4. Database schema

- **Schema**: `bodymetrics` (PostgreSQL, created by FluentMigrator, mapped by EF Core)
- **Tables**:

| Table | Columns | Indexes |
|-------|---------|---------|
| `bodymetrics_WeightMeasurements` | `Id` (uuid PK), `UserId`, `Weight` decimal(5,2), `MeasuredAt` timestamptz | `Ix_WeightMeasurement_UserId_MeasuredAt` |
| `bodymetrics_Perimeters` | `Id`, `UserId`, `MeasuredAt` timestamptz, `Neck`, `RightArmRelaxed`, `RightArmTensioned`, `LeftArmRelaxed`, `LeftArmTensioned`, `Chest`, `Shoulder`, `Waist`, `Hip`, `Abs`, `RightThigh`, `LeftThigh`, `RightCalf`, `LeftCalf` (decimal(5,2) null) | `Ix_Perimeters_UserId_MeasuredAt` |
| `bodymetrics_SkinFolds` | `Id`, `UserId`, `MeasuredAt` timestamptz, `Abdominal`, `Suprailiac`, `Tricipital`, `Subscapular`, `Thigh`, `Calf` (decimal(5,2) null) | `Ix_SkinFolds_UserId_MeasuredAt` |

- **Mapping**: the value objects are `ComplexProperty` (same row). Their private parameterless constructor is used by EF; the aggregates set `Measures` after construction because EF cannot bind complex properties to constructor parameters.
- **Migrations**: `Infrastructure/Migrations/2026101410xx_*` (schema + one per table). **Every module shares one FluentMigrator runner and the `VersionInfo` table**, so versions must be unique across the whole project: keep using `2026101410xx` and following numbers for this module.

## 5. Folder structure

```
bodymetrics/
├── LightWeight.BodyMetrics.Domain/
│   ├── Aggregates/        # WeightMeasurement, Perimeters, SkinFolds
│   ├── ValueObjects/      # PerimeterMeasures, SkinFoldMeasures, MeasurementRules
│   ├── Events/            # Registered / Corrected / Deleted domain events per kind
│   ├── Exceptions/        # BodyMetricsDomainException + measurement exceptions
│   ├── Enum/              # Sex
│   ├── Repositories/      # I{WeightMeasurement,Perimeters,SkinFolds}Repository
│   └── Uow/               # IBodyMetricsUnitOfWork
├── LightWeight.BodyMetrics.Application/
│   ├── Commands/{Weights,Perimeters,SkinFolds}/{Register,Correct,Delete}…   # command + validator + handler
│   ├── Commands/{Perimeters,SkinFolds}/{PerimeterValues,SkinFoldValues}.cs  # client values → value object
│   ├── Queries/{Weights,Perimeters,SkinFolds}/Get…History                   # latest 10
│   ├── Events/            # domain events → integration events (IEventPublisher)
│   ├── Exceptions/        # MeasurementNotFoundApplicationException
│   ├── Utils/UtcDate.cs
│   └── DependencyInjection.cs   # AddBodyMetricsApplication
├── LightWeight.BodyMetrics.Infrastructure/
│   ├── Configurations/    # EF configurations
│   ├── Migrations/        # FluentMigrator
│   ├── Persistence/       # BodyMetricsDbContext, UnitOfWork, Repositories/
│   └── DependencyInjection.cs   # AddBodyMetricsInfrastructure, AddBodyMetricsModule
├── LightWeight.BodyMetrics.Api/
│   ├── DTOs/              # WeightRequest, PerimetersRequest, SkinFoldsRequest
│   └── BodyMetricsModule.cs     # MapBodyMetricsEndpoints (/api/bodymetrics)
└── testing/LightWeight.BodyMetrics.UnitTests/   # Domain/ and Application/ (xUnit + NSubstitute)
```

Host: `App/LightWeight.App/Program.cs` calls `AddBodyMetricsModule(configuration)` and `MapBodyMetricsEndpoints()`.

Frontend: `frontend/LightWeight/src/app/Modules/BodyMetrics` — route `/bodymetrics`, one generic `measures-panel` driven by `UI/measurement-config.ts` (fields, unit and limits per kind; keep its limits in sync with the domain).

## 6. Dependencies

- **Depends on**: `LightWeight.Shared` (building blocks, mediator, messaging, `IClock`, contracts).
- **Shared services**: `IMediator`, `IEventPublisher`, `IEventDispatcher`, `IClock` and the validation pipeline are registered with `TryAdd`/existence checks, because other modules register them too (a duplicated pipeline behaviour would validate each command twice).
- **Integration events published**: `bodymetrics.{weight,perimeters,skinfolds}.{recorded,deleted}.v1` (see `BodyMetrics.md`).
- **Integration events consumed**: none.
- **Expected consumers**: Dashboard.

## 7. Current state

**Done:** domain with value objects and events, integration contracts, persistence (schema, migrations, repositories, UnitOfWork with event dispatch), commands/queries/validators, API, host registration, frontend (register, latest 10, correct, delete), unit tests for domain and application.

**Pending:**
- Global exception handler (errors reach the client as 500).
- Exposing the estimated fat percentage (needs the sex from UserProfile).
- Dashboard consuming the events.

## 8. Things NOT to do in this module

- Do NOT create measurements without the factory methods, nor bypass the value objects: they hold the validation.
- Do NOT remove an aggregate without calling `Delete()` first (the deleted event would be lost).
- Do NOT add analytics or long history views here (trends, averages, charts): they belong to the Dashboard, fed by the integration events.
- Do NOT change an integration event contract in place: add a new version (`…v2`) and keep the old one while it has consumers.
- Do NOT reuse migration versions of other modules (shared `VersionInfo`).
- Do NOT use `DateTime.UtcNow` in handlers or store non-UTC dates.
- Do NOT reference other modules' domains: communicate through integration events only.
