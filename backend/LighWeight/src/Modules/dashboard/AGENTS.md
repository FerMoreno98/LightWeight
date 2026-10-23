# AGENTS.md — Module: Dashboard

## 1. Module purpose

Read model of the application. It receives the integration events of the other modules and keeps them as projections ready to be charted. Today it projects **BodyMetrics** (weight, perimeters, skinfolds; charts in the "Evolución" tab of the BodyMetrics page) and **Training** (finished sessions with their sets; charts in the home and in the "Progreso" section). It can rebuild the projections of a user by asking the source modules to publish their state again. See `Dashboard.md` for the functional description.

## 2. Shape of the module

**No Domain layer, on purpose.** There are no aggregates nor invariants: the dashboard only copies what happened in other modules. The layers are:

- **Application**: projections (plain classes), the store interface, the projectors (`IIntegrationEventHandler<T>`), the `RebuildProjections` command, the read queries and the calculations. It references `LightWeight.Shared` directly.
- **Infrastructure**: `DashboardDbContext`, `BodyMetricsProjectionStore`, `TrainingProjectionStore`, migrations.
- **Api**: `MapDashboardEndpoints`.

### Projections (`Application/Projections/BodyMetrics`)
- `WeightProjection`, `PerimetersProjection`, `SkinFoldsProjection`: key `MeasurementId` (the id of the source measurement), plus `UserId` and `MeasuredAt`.
- `IBodyMetricsProjectionStore`: `Upsert…`/`Delete…` per kind and `ClearUserAsync`. **Each operation saves at once** (one event, one write). Reads: `Get{Weights,Perimeters,SkinFolds}Async(userId)`, the whole history from the oldest (`AsNoTracking`).

### Projectors (`Application/Projectors/BodyMetrics`)
- `WeightProjector`, `PerimetersProjector`, `SkinFoldsProjector`: `…Recorded` → upsert, `…Deleted` → delete.

### Queries (`Application/Queries/BodyMetrics`)
- `GetWeightEvolution` → `{ Points[{MeasuredAt, Weight}], WeeklyAverages[{WeekStart, Average, Count}] }`.
- `GetPerimetersEvolution` / `GetSkinFoldsEvolution` → one point per measurement with every field (null if not measured; skinfolds include `Summatory`).

### Training (`Projections/Training`, `Projectors/Training`, `Queries/Training`, `Calculations/Training`)
- `TrainingSessionProjection` (key `SessionId`, with the cycle ids and the wellbeing) and `TrainingSetProjection` (key `SetId`, with what was performed and the facts of its planned set).
- `TrainingSessionProjector`: `training.session.finished.v1` → `ITrainingProjectionStore.UpsertSessionAsync(session, sets)`, which upserts the session and **replaces its sets in one save**.
- Queries: `GetTrainingVolume`, `GetSessionsPerformance`, `GetExercisesProgress`, `GetExerciseProgress` (null → 404).
- Calculations: `OneRepMax`, `RepZones`, `ExerciseHistory` (an exercise in a session: its sets, zone and estimates; plus `Trend`), `SetFatigue`, `SessionPerformance`, `WeeklyVolume`. Formulas and limits in `Dashboard.md` → Calculations.

### Calculations (`Application/Calculations`)
- `WeeklyAverages`: pure and static. Groups by ISO week (Monday 00:00 UTC to Sunday), average rounded to 2 decimals, weeks without values do not appear.
- Calculated **when reading**, not stored: a few hundred points per user, and nothing more to keep in sync or rebuild.
- It lives in Application and not in a Domain project because it protects no invariant (it only reads valid data). If calculations with rules of their own appear (fat %, energy balance), `Calculations/` can be moved to a `LightWeight.Dashboard.Domain` project as is.

## 3. Non-obvious rules

- **Projectors must be idempotent and must not throw for existing or missing data.** `…Recorded` arrives on register, on correct and during a rebuild: always upsert. Deleting a missing row does nothing. They run inside the request of the source module, after its commit: an exception here returns an error to a user whose measurement was saved anyway.
- **Upsert = find by id, then overwrite every value or add** (`CurrentValues.SetValues`). A correction can turn a value into null, so never merge field by field.
- **Deletes filter by `MeasurementId` and `UserId`**, so an event can never remove a row of another user.
- **The training sets are matched by `SetId` when a session is written again** (overwrite / add / remove the missing ones), never deleted and re-inserted: EF Core cannot track a deleted and a new entity with the same key in one save.
- **The analysis rules belong here, not in training.** Training publishes facts (performed set + its planned set); which muscles a set counts (planned ones, or the exercise's for an extra set), the zones (≤ 6 / 7–15 / ≥ 16), the 1RM limits (less precise over 15, none over 30) are constants of `Calculations/Training`. Changing them needs no change in training nor a new event version.
- **Effort zones are never mixed**: fatigue, trend and session performance only compare an exercise with itself **in the same zone**.
- **Rebuild order matters**: `ClearUserAsync` first, then publish `RebuildRequestedIntegrationEvent`. Emptying afterwards would delete what the sources just published. The tests check the order (`Received.InOrder`).
- **The dashboard does not know its sources.** The rebuild only publishes `dashboard.rebuild.requested.v1`; each source module subscribes and republishes its `…Recorded` events. Never query another module's tables or reference its assemblies.
- **Projections only store what the event carries.** Calculations that need other data (e.g. fat percentage, which needs the user's sex) wait until that data arrives through an event.
- **Dates are UTC** (`timestamp with time zone`). They come already normalized from the source events.
- **Time comes from `IClock`**, never `DateTime.UtcNow`.
- **The shared mediator does not validate queries**: the query handlers check the user themselves.
- **Weeks are cut in UTC.** Morning weigh-ins in Spain never change week; if that ever matters, pass the user's time zone to `WeeklyAverages`.

## 4. Database schema

- **Schema**: `dashboard` (PostgreSQL, created by FluentMigrator, mapped by EF Core)
- **Tables**:

| Table | Columns | Indexes |
|-------|---------|---------|
| `dashboard_BodyWeights` | `MeasurementId` (uuid PK), `UserId`, `MeasuredAt` timestamptz, `Weight` decimal(5,2) | `Ix_BodyWeights_UserId_MeasuredAt` |
| `dashboard_BodyPerimeters` | `MeasurementId` (PK), `UserId`, `MeasuredAt`, `Neck`, `RightArmRelaxed`, `RightArmTensioned`, `LeftArmRelaxed`, `LeftArmTensioned`, `Chest`, `Shoulder`, `Waist`, `Hip`, `Abs`, `RightThigh`, `LeftThigh`, `RightCalf`, `LeftCalf` (decimal(5,2) null) | `Ix_BodyPerimeters_UserId_MeasuredAt` |
| `dashboard_TrainingSessions` | `SessionId` (uuid PK), `UserId`, `MacrocycleId`, `MesocycleId`, `MicrocycleId`, `WeekNumber`, `Name`, `StartedAt`, `FinishedAt`, `Motivation`, `Sleep`, `Doms` | `Ix_TrainingSessions_UserId_StartedAt` |
| `dashboard_TrainingSets` | `SetId` (uuid PK), `SessionId`, `UserId`, `Order`, `ExerciseId`, `ExerciseName`, `Repetitions`, `Weight` decimal(8,2), `Rpe` decimal(3,1), `IsBodyWeight`, `Technique?`, `IsPlanned`, `ExerciseMuscleGroups` text[], `PlannedMuscleGroups` text[]?, `PlannedMinReps?`, `PlannedMaxReps?` | `Ix_TrainingSets_UserId_ExerciseId`, `Ix_TrainingSets_SessionId` |
| `dashboard_BodySkinFolds` | `MeasurementId` (PK), `UserId`, `MeasuredAt`, `Abdominal`, `Suprailiac`, `Tricipital`, `Subscapular`, `Thigh`, `Calf`, `Summatory` (decimal(5,2) null) | `Ix_BodySkinFolds_UserId_MeasuredAt` |

- Unlike bodymetrics, the skinfold **summatory is stored**: it is what the charts show.
- **Migrations**: `Infrastructure/Migrations/2026101510xx_*` (bodymetrics) and `2026102310xx_*` (training). **Every module shares one FluentMigrator runner and the `VersionInfo` table**: keep using these ranges and following numbers for this module.

## 5. Folder structure

```
dashboard/
├── LightWeight.Dashboard.Application/
│   ├── Projections/{BodyMetrics,Training}/   # projections + stores
│   ├── Projectors/{BodyMetrics,Training}/    # integration event handlers
│   ├── Commands/RebuildProjections/
│   ├── Queries/BodyMetrics/       # Get{Weight,Perimeters,SkinFolds}Evolution
│   ├── Queries/Training/          # volume, sessions performance, exercises progress
│   ├── Calculations/              # WeeklyAverages; Training/: OneRepMax, RepZones, ExerciseHistory, SetFatigue, SessionPerformance, WeeklyVolume
│   └── DependencyInjection.cs     # AddDashboardApplication
├── LightWeight.Dashboard.Infrastructure/
│   ├── Configurations/            # EF configurations of the projections
│   ├── Migrations/                # FluentMigrator
│   ├── Persistence/               # DashboardDbContext, BodyMetricsProjectionStore, TrainingProjectionStore
│   └── DependencyInjection.cs     # AddDashboardInfrastructure, AddDashboardModule
├── LightWeight.Dashboard.Api/
│   └── DashboardModule.cs         # MapDashboardEndpoints (/api/dashboard): POST /rebuild, GET /bodymetrics/{weight,perimeters,skinfolds}, GET /training/{volume,sessions,exercises,exercises/{id}}
└── testing/LightWeight.Dashboard.UnitTests/   # Application/ + Fakes/ (in-memory stores, TrainingData builder)
```

Host: `App/LightWeight.App/Program.cs` calls `AddDashboardModule(configuration)` and `MapDashboardEndpoints()`. The `Dockerfile` copies the three `.csproj` (there is no Domain project).

Frontend: `frontend/LightWeight/src/app/Modules/Dashboard` — `data/dashboard-api.service.ts`, `state/body-metrics-charts.store.ts`, `UI/line-chart` (generic time chart over ng2-charts' `BaseChartDirective`, linear X axis with timestamps), `UI/body-metrics-charts` (the four charts + "Recalcular") and `UI/chart-theme.ts` (colors). The charts are embedded in the "Evolución" tab of the BodyMetrics page; Chart.js is registered in the `providers` of `BodyMetricsCharts` so it only loads with that tab. Series colors: the first 4 slots of a categorical palette validated for color blindness on the card surface — never more than 4 series per chart, and a field keeps its color while selected (`UI/series-selection.ts`).

Training: `UI/training-overview` (home: session performance as `UI/bar-chart` stacked over the wellbeing `line-chart`, sharing their dates through `xDomain`; weekly volume), `Dashboard.Routes.ts` → `/progress` (`UI/progress-list`, `UI/exercise-progress`), stores `training-overview.store.ts` and `exercise-progress.store.ts`, Spanish labels in `UI/training-labels.ts`. Line and bar charts share `UI/time-chart-options.ts` (time axis, theme, tooltip with title/note/extra lines per point).

## 6. Dependencies

- **Depends on**: `LightWeight.Shared` (mediator, messaging, `IClock`, contracts, migrations).
- **Shared services**: `IMediator`, `IEventPublisher`, `IClock` and the validation pipeline are registered with `TryAdd`/existence checks, like in the other modules.
- **Integration events consumed**: `bodymetrics.{weight,perimeters,skinfolds}.{recorded,deleted}.v1`, `training.session.finished.v1`.
- **Integration events published**: `dashboard.rebuild.requested.v1` (answered by BodyMetrics and Training).

## 7. Current state

**Done:** BodyMetrics and Training projections, rebuild (answered by both sources), read queries and calculations (weekly averages; 1RM, zones, volume, fatigue, session performance, trend), host registration, unit tests, frontend charts (Evolución tab, home, Progreso).

**Pending:**
- Macrocycle/mesocycle filter: optional `?macrocycleId=`/`?mesocycleId=` on the training endpoints (the ids are already stored), cycle events from Training for the selector, and their date ranges for the body measurement charts.
- Fat percentage (needs the sex from UserProfile).
- Structured logs in the event flow; subscriber failures logged instead of returning 500 to the user.

## 8. Things NOT to do in this module

- Do NOT add a Domain layer or business rules: decisions belong to the source modules.
- Do NOT write projectors that throw for duplicated or missing data, nor that depend on the order of arrival beyond upsert/delete.
- Do NOT read other modules' tables or reference their projects: only integration events.
- Do NOT emit events other than the rebuild request.
- Do NOT reuse migration versions of other modules (shared `VersionInfo`).
- Do NOT introduce an outbox (or any async delivery) without revisiting the decision in `Dashboard.md` → Open Notes: rare inconsistencies are accepted and repaired with the rebuild.
