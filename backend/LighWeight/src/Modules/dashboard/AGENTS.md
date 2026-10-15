# AGENTS.md — Module: Dashboard

## 1. Module purpose

Read model of the application. It receives the integration events of the other modules and keeps them as projections ready to be charted. Today it projects **BodyMetrics** (weight, perimeters, skinfolds); the read queries and the charts come in the next iteration. It can rebuild the projections of a user by asking the source modules to publish their state again. See `Dashboard.md` for the functional description.

## 2. Shape of the module

**No Domain layer, on purpose.** There are no aggregates nor invariants: the dashboard only copies what happened in other modules. The layers are:

- **Application**: projections (plain classes), the store interface, the projectors (`IIntegrationEventHandler<T>`) and the `RebuildProjections` command. It references `LightWeight.Shared` directly.
- **Infrastructure**: `DashboardDbContext`, `BodyMetricsProjectionStore`, migrations.
- **Api**: `MapDashboardEndpoints`.

### Projections (`Application/Projections/BodyMetrics`)
- `WeightProjection`, `PerimetersProjection`, `SkinFoldsProjection`: key `MeasurementId` (the id of the source measurement), plus `UserId` and `MeasuredAt`.
- `IBodyMetricsProjectionStore`: `Upsert…`/`Delete…` per kind and `ClearUserAsync`. **Each operation saves at once** (one event, one write).

### Projectors (`Application/Projectors/BodyMetrics`)
- `WeightProjector`, `PerimetersProjector`, `SkinFoldsProjector`: `…Recorded` → upsert, `…Deleted` → delete.

## 3. Non-obvious rules

- **Projectors must be idempotent and must not throw for existing or missing data.** `…Recorded` arrives on register, on correct and during a rebuild: always upsert. Deleting a missing row does nothing. They run inside the request of the source module, after its commit: an exception here returns an error to a user whose measurement was saved anyway.
- **Upsert = find by id, then overwrite every value or add** (`CurrentValues.SetValues`). A correction can turn a value into null, so never merge field by field.
- **Deletes filter by `MeasurementId` and `UserId`**, so an event can never remove a row of another user.
- **Rebuild order matters**: `ClearUserAsync` first, then publish `RebuildRequestedIntegrationEvent`. Emptying afterwards would delete what the sources just published. The tests check the order (`Received.InOrder`).
- **The dashboard does not know its sources.** The rebuild only publishes `dashboard.rebuild.requested.v1`; each source module subscribes and republishes its `…Recorded` events. Never query another module's tables or reference its assemblies.
- **Projections only store what the event carries.** Calculations that need other data (e.g. fat percentage, which needs the user's sex) wait until that data arrives through an event.
- **Dates are UTC** (`timestamp with time zone`). They come already normalized from the source events.
- **Time comes from `IClock`**, never `DateTime.UtcNow`.

## 4. Database schema

- **Schema**: `dashboard` (PostgreSQL, created by FluentMigrator, mapped by EF Core)
- **Tables**:

| Table | Columns | Indexes |
|-------|---------|---------|
| `dashboard_BodyWeights` | `MeasurementId` (uuid PK), `UserId`, `MeasuredAt` timestamptz, `Weight` decimal(5,2) | `Ix_BodyWeights_UserId_MeasuredAt` |
| `dashboard_BodyPerimeters` | `MeasurementId` (PK), `UserId`, `MeasuredAt`, `Neck`, `RightArmRelaxed`, `RightArmTensioned`, `LeftArmRelaxed`, `LeftArmTensioned`, `Chest`, `Shoulder`, `Waist`, `Hip`, `Abs`, `RightThigh`, `LeftThigh`, `RightCalf`, `LeftCalf` (decimal(5,2) null) | `Ix_BodyPerimeters_UserId_MeasuredAt` |
| `dashboard_BodySkinFolds` | `MeasurementId` (PK), `UserId`, `MeasuredAt`, `Abdominal`, `Suprailiac`, `Tricipital`, `Subscapular`, `Thigh`, `Calf`, `Summatory` (decimal(5,2) null) | `Ix_BodySkinFolds_UserId_MeasuredAt` |

- Unlike bodymetrics, the skinfold **summatory is stored**: it is what the charts show.
- **Migrations**: `Infrastructure/Migrations/2026101510xx_*`. **Every module shares one FluentMigrator runner and the `VersionInfo` table**: keep using `2026101510xx` and following numbers for this module.

## 5. Folder structure

```
dashboard/
├── LightWeight.Dashboard.Application/
│   ├── Projections/BodyMetrics/   # projections + IBodyMetricsProjectionStore
│   ├── Projectors/BodyMetrics/    # integration event handlers
│   ├── Commands/RebuildProjections/
│   └── DependencyInjection.cs     # AddDashboardApplication
├── LightWeight.Dashboard.Infrastructure/
│   ├── Configurations/            # EF configurations of the projections
│   ├── Migrations/                # FluentMigrator
│   ├── Persistence/               # DashboardDbContext, BodyMetricsProjectionStore
│   └── DependencyInjection.cs     # AddDashboardInfrastructure, AddDashboardModule
├── LightWeight.Dashboard.Api/
│   └── DashboardModule.cs         # MapDashboardEndpoints (/api/dashboard)
└── testing/LightWeight.Dashboard.UnitTests/   # Application/ + Fakes/ (in-memory store)
```

Host: `App/LightWeight.App/Program.cs` calls `AddDashboardModule(configuration)` and `MapDashboardEndpoints()`. The `Dockerfile` copies the three `.csproj` (there is no Domain project).

## 6. Dependencies

- **Depends on**: `LightWeight.Shared` (mediator, messaging, `IClock`, contracts, migrations).
- **Shared services**: `IMediator`, `IEventPublisher`, `IClock` and the validation pipeline are registered with `TryAdd`/existence checks, like in the other modules.
- **Integration events consumed**: `bodymetrics.{weight,perimeters,skinfolds}.{recorded,deleted}.v1`.
- **Integration events published**: `dashboard.rebuild.requested.v1` (answered by BodyMetrics).

## 7. Current state

**Done:** BodyMetrics projections (tables, store, projectors), rebuild (`POST /api/dashboard/rebuild` + BodyMetrics answering it), host registration, unit tests.

**Pending:**
- Read queries and endpoints for the charts; frontend screen with Chart.js and the "Recalcular" button.
- Training as a source (its integration events + subscription to the rebuild).
- Fat percentage (needs the sex from UserProfile).
- Outbox in the shared messaging (reliable events, real replay).

## 8. Things NOT to do in this module

- Do NOT add a Domain layer or business rules: decisions belong to the source modules.
- Do NOT write projectors that throw for duplicated or missing data, nor that depend on the order of arrival beyond upsert/delete.
- Do NOT read other modules' tables or reference their projects: only integration events.
- Do NOT emit events other than the rebuild request.
- Do NOT reuse migration versions of other modules (shared `VersionInfo`).
