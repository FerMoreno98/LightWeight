# AGENTS.md — Module: Training

## 1. Module purpose

This module handles strength and bodybuilding training planning and logging. It models the full training hierarchy: `Macrocycle` (long block, months) → `Mesocycle` (weeks) → `Microcycle` (training week) → `TrainingSession` (a single session) → `Set` (each performed set). It also provides `Program` → `TrainingTemplate` as reusable plans and `Exercise` as a global (seeded) exercise catalog. Every finished session is published as an integration event so the Dashboard can analyse the history (see `Training.md` → Events Emitted).

## 2. Aggregates and entities

### `Macrocycle` (AggregateRoot of the cycle hierarchy)
- **Properties**: `UserId`, `StartedAt`, `FinishedAt?` (null while active), `Stage` (TrainingStage: Bulk/Cut/Maintenance), `Comments?`
- **Child entities**: `Mesocycle` → `Microcycle`. The whole hierarchy is one aggregate: mesocycles and weeks are only created and changed through the macrocycle
- **Invariants** (cycles are sequential, there are no planned dates):
  - A user has at most one active macrocycle (checked in `Macrocycle.Create` with the active one loaded by the handler + partial unique index `Ux_Macrocycle_ActivePerUser`)
  - A macrocycle has at most one active mesocycle, and a mesocycle at most one active week (`ActiveMesocycleAlreadyExists` / `ActiveMicrocycleAlreadyExists` + partial unique indexes)
  - A finished cycle is closed: no children can be added and it cannot be finished again
  - Week numbers are assigned by the mesocycle (max + 1)
- **Behaviour methods**: `PlanMesocycle`, `PlanMicrocycle(mesocycleId, templateId, now)`, `FinishMesocycle`, `FinishMicrocycle`, `Finish` (cascades to the active mesocycle and its active week with the same date). Read helpers: `ActiveMesocycle`, `ActiveMicrocycle`
- **DB table**: `training_Macrocycles`

### `Mesocycle` (Entity, child of `Macrocycle`)
- **Properties**: `MacrocycleId`, `UserId`, `ProgramId`, `MotivationLevel` (1-10), `Injuries?`, `Comments?`, `StartedAt`, `FinishedAt?`
- Follows a `Program`: its weeks must use templates of that program (checked by the application handler, since `Program` is another aggregate)
- **DB table**: `training_Mesocycles` (FK → Macrocycle cascade, FK → Program restrict)

### `Microcycle` (Entity, child of `Mesocycle`)
- **Properties**: `MesocycleId`, `UserId`, `TrainingTemplateId`, `WeekNumber`, `StartedAt`, `FinishedAt?`
- The active week of the active mesocycle of the active macrocycle is the "current week" shown in the home (`GET /microcycle/current`)
- **DB table**: `training_Microcycles` (FK → Mesocycle cascade, FK → TrainingTemplate restrict, unique `(MesocycleId, WeekNumber)`)

### `TrainingSession` (AggregateRoot)
- **Properties**: `MicrocycleId`, `UserId`, `TemplateSessionId?`, `Name`, `StartAt`, `FinishedAt?` (null while in progress), `Duration`, `Comments?`, `MotivationLevel`, `SleepLevel`, `DOMSLevel` (1-10)
- **Child entities**: `Set` (only created and changed through the session)
- **Invariants**:
  - Only the week in progress can be trained, with a session of the week's template (checked by `CreateTrainingSession`)
  - A session of the template is trained at most once per week (handler + partial unique index `Ux_TrainingSession_TemplateSessionPerWeek`)
  - A finished session is read only; `Finish(now)` stores `FinishedAt` and `Duration = now - StartAt`
  - A planned set (`TemplateSetId`) is covered at most once per session; corrections go through `UpdateSet`
- **Behaviour methods**: `RegisterSet`, `UpdateSet`, `RemoveSet`, `Finish`
- **Domain events it emits**: `TrainingSessionFinishedDomainEvent` (in `Finish`), translated into `training.session.finished.v1`
- **Repository**: `ITrainingSessionRepository`, including `GetLastSetsOfExercisesAsync` (last performance of each exercise in a set of weeks, used for "Anterior")
- **DB table**: `training_TrainingSessions` (FK → Microcycle cascade, FK → TemplateSession restrict)

### `Set` (Entity, child of `TrainingSession`)
- **Properties**: `ExerciseId` (the exercise actually performed), `TemplateSetId?`, `Repetitions`, `Weight`, `RPE` (1-10), `IsBodyWeight`, `AdvanceTrainingTechniques`, `SuperSetGroupId?`
- Real sets are not tied to the plan: with `TemplateSetId` they cover a planned set (with the planned exercise or a substitute), without it they are extra sets. Technique and superset are copied from the planned set
- **DB table**: `training_Sets` (FK → TrainingSession cascade, FK → Exercise, FK → TemplateSet restrict; technique stored as `AdvanceTrainingTechniques_IsDropSet/IsCluster/IsMyoRep`)

### `Program` (AggregateRoot)
- **Properties**: `UserId`, `Name`, `Periodization`, `AimMuscleGroups` (List\<MuscleGroups\>)
- **Invariants**:
  - Belongs to a single user (`UserId`, cannot be empty → `UserIdEmptyDomainException`)
  - `Name` cannot be empty → `NameEmptyDomainException`
  - Is the only entry point to the template hierarchy (`TrainingTemplate` → `TemplateSession` → `TemplateSet`)
  - `trainingTemplates` only exposes non-deleted templates
- **Child entities**: `TrainingTemplate`
- **Behaviour methods**:
  - `AddTrainingTemplate(TrainingTemplate)`
  - `DeleteTrainingTemplate(Guid trainingTemplateId, DateTime now)`: soft deletes the template and its whole hierarchy
- **Domain events it emits**: None

### `TrainingTemplate` (Entity, child of `Program`)
- **Properties**: `TrainingDistribution`, `VolumeLandmark`, `DurationInDays`, `Order`, `IsDeleted`, `DeletedAt?`
- **Invariants**:
  - `VolumeLandmark` and `TrainingDistribution` must be defined enum values
  - `TemplateSessions` only exposes non-deleted sessions
- **Child entities**: `TemplateSession`
- **Behaviour methods**:
  - `DeleteSessionTemplate(Guid templateSessionId, DateTime now)`: soft deletes the session and its sets
  - `GetNumberOfSeriesPerGroup()`: ignores soft deleted sessions and sets
- **DB table**: `training_TrainingTemplates`

### `Exercise` (AggregateRoot)
- **Properties**: `Name`, `IsBilateral`, `AimMuscleGroups` (List\<MuscleGroups\>)
- **Invariants**:
  - `AimMuscleGroups` lists the muscle groups this exercise can work (not necessarily all are targeted in every set)
  - `IsBilateral` indicates whether the exercise involves both limbs simultaneously
- **Child entities**: None
- **Domain events it emits**: None
- **Design note**: Exercises are a global catalog seeded by `Infrastructure/Persistence/Seed/ExerciseSeeder.cs` (no `UserId`, no create command). The `Set` and `TemplateSet` entities reference `ExerciseId` rather than embedding the full value.
- **DB table**: `training_Exercises` (`_aimMuscleGroups` stored as `jsonb`)

### `TemplateSession` (Entity, child of `TrainingTemplate`)
- **Properties**: `Name`, `IsDeleted`, `DeletedAt?`
- Represents a planned session within a template
- Contains a collection of `TemplateSet` (`TemplateExercises` only exposes non-deleted sets)
- `DeleteTemplateSet(Guid templateSetId, DateTime now)`: soft deletes a set
- **DB table**: `training_TemplateSessions` (FK → TrainingTemplate)

### `TemplateSet` (Entity, child of `TemplateSession`)
- **Properties**: `ExerciseId`, `RepetitionRange` (ValueObject: Min/Max), `ExpectedRPE`, `AdvanceTrainingTechniques`, `SuperSetGroupId?`, `AimMuscleGroups`, `IsDeleted`, `DeletedAt?`
- Uses `RepetitionRange` (min-max) instead of a fixed rep count to allow autoregulation
- **DB table**: `training_TemplateSets` (FK → TemplateSession, FK → Exercise; `RepetitionRange` and `AdvanceTrainingTechniques` columns embedded)

## 3. Non-obvious business rules

- **Supersets are modeled via `SuperSetGroupId`**: There is no `IsSuperSet` boolean. Sets sharing the same `SuperSetGroupId` belong to the same superset (pairs, triplets...). They are performed alternating their exercises round by round (round r = the r-th set of each exercise).
  - **Planned**: only `TemplateSession.CreateSuperSet(setIds)` / `RemoveSuperSet(groupId)` change them (clients cannot send a group id when creating or updating a set). A planned superset needs at least two different exercises and is dissolved when it is left with one (also when deleting sets). `Duplicate` gives the copies new group ids.
  - **Real**: the client sends the group each set was performed in (planned, improvised in the session or none) and `TrainingSession.AssignSuperSet` regroups recorded sets. No minimum of exercises is enforced, because sets are recorded one by one.
- **At most one advanced technique per set**: `AdvanceTrainingTechniques` enforces that only one of `IsDropSet`, `IsCluster`, or `IsMyoRep` can be `true`. Attempting to create with more than one active throws `AdvanceTrainingTechniquesExceptions`.
- **Template vs. real sessions**: `TrainingTemplate`, `TemplateSession`, and `TemplateSet` are planning aggregates and never reference real session IDs. Real `TrainingSession` and its `Set` are created independently, possibly copying structure from a template.
- **Templates use soft delete**: `TrainingTemplate`, `TemplateSession` and `TemplateSet` are never physically deleted. `Microcycle.TrainingTemplateId`, `TrainingSession.TemplateSessionId` and `Set.TemplateSetId` point to these inner entities of the `Program` aggregate, so a hard delete would leave dangling references in the training history.
  - Deleting goes through the parent (`Program.DeleteTrainingTemplate`, `TrainingTemplate.DeleteSessionTemplate`, `TemplateSession.DeleteTemplateSet`), which sets `IsDeleted`/`DeletedAt`. `MarkAsDeleted` is `internal` and cascades down the hierarchy.
  - **Never remove an item from the private collections** (`_trainingTemplates`, `_templateSessions`, `_templateExercises`): with the required FK + cascade configuration EF Core would physically delete the row.
  - Public collections and the series-count methods skip deleted items, so they behave correctly inside the same unit of work before reloading.
  - EF Core applies a global query filter (`HasQueryFilter(t => !t.IsDeleted)`) on the three entities, also inside `Include`. Use `IgnoreQueryFilters()` only when reading history (e.g. resolving the template of an old `TrainingSession`).
- **The finished session is published with everything inside** (`TrainingSessionEventFactory` + `TrainingIntegrationEventMapper`, the only place that builds the contract). Each set carries the facts of its planned set (muscle groups and repetition range) read **including soft deleted planned sets** (`GetPlannedSetsIncludingDeletedAsync`); interpreting them (volume, zones, fatigue) is the Dashboard's job. A finished session is read only, so no correction/deletion events exist.
- **Rebuild**: on `dashboard.rebuild.requested.v1`, `RebuildRequestedIntegrationEventHandler` republishes every finished session of the user (`GetFinishedOfAUserAsync`). Keep it read only.
- **`RepetitionRange.Create` silently swaps values**: If `max < min`, the values are swapped instead of throwing. A `DomainException` may be more appropriate for invalid input.

## 4. Database schema

- **Schema**: `training` (PostgreSQL, managed by FluentMigrator and EF Core)
- **Tables**:

| Table | Columns | FK |
|-------|---------|----|
| `training_Macrocycles` | `Id` (guid PK), `UserId`, `StartedAt`, `FinishedAt?`, `Stage` (varchar 20), `Comments?` | — |
| `training_Mesocycles` | `Id` (guid PK), `MacrocycleId`, `UserId`, `ProgramId`, `MotivationLevel`, `Injuries?`, `Comments?`, `StartedAt`, `FinishedAt?` | `MacrocycleId` → Macrocycles (cascade), `ProgramId` → Programs |
| `training_Microcycles` | `Id` (guid PK), `MesocycleId`, `UserId`, `TrainingTemplateId`, `WeekNumber`, `StartedAt`, `FinishedAt?` | `MesocycleId` → Mesocycles (cascade), `TrainingTemplateId` → TrainingTemplates |
| `training_TrainingSessions` | `Id` (guid PK), `MicrocycleId`, `UserId`, `TemplateSessionId?`, `Name` (200), `StartAt`, `FinishedAt?`, `Duration` (interval), `Comments?`, `MotivationLevel`, `SleepLevel`, `DOMSLevel` | `MicrocycleId` → Microcycles (cascade), `TemplateSessionId` → TemplateSessions |
| `training_Sets` | `Id` (guid PK), `TrainingSessionId`, `ExerciseId`, `TemplateSetId?`, `Repetitions`, `IsBodyWeight`, `Weight` (decimal 8,2), `RPE` (decimal 3,1), `SuperSetGroupId?`, `AdvanceTrainingTechniques_IsDropSet/IsCluster/IsMyoRep` | `TrainingSessionId` → TrainingSessions (cascade), `ExerciseId` → Exercises, `TemplateSetId` → TemplateSets |
| `training_Programs` | `Id` (guid PK), `UserId`, `Name` (200), `Periodization` (varchar 20), `AimMuscleGroups` (jsonb) | — |
| `training_TrainingTemplates` | `Id` (guid PK), `ProgramId`, `TrainingDistribution` (varchar 20), `VolumeLandmark`, `DurationInDays`, `Order`, `IsDeleted` (bool, default false), `DeletedAt?` | `ProgramId` → Programs (cascade) |
| `training_TemplateSessions` | `Id` (guid PK), `TrainingTemplateId`, `Name` (200), `IsDeleted` (bool, default false), `DeletedAt?` | `TrainingTemplateId` → TrainingTemplates (cascade) |
| `training_TemplateSets` | `Id` (guid PK), `TemplateSessionId`, `ExerciseId`, `ExpectedRPE` (decimal 3,1), `SuperSetGroupId?`, `AimMuscleGroups` (jsonb), `RepetitionRange_Min`, `RepetitionRange_Max`, `AdvanceTrainingTechniques`, `IsDeleted` (bool, default false), `DeletedAt?` | `TemplateSessionId` → TemplateSessions (cascade), `ExerciseId` → Exercises (cascade) |
| `training_Exercises` | `Id` (guid PK), `Name` (200), `IsBilateral`, `AimMuscleGroups` (jsonb) | — |

- **Migrations**: located in `Infrastructure/Migrations/`, naming convention `{yyyyMMddHHmm}_{Description}.cs` (soft delete columns added in `202607291213_AddSoftDeleteToTemplates`)
- **Program restructure** (`202607291214_RestructureTrainingAroundProgram`): creates `training_Programs` (one Program per pre-existing template, reusing its Id), moves `UserId`/`Name` out of `training_TrainingTemplates` (now `ProgramId`, `DurationInDays`, `Order`), drops `Macrocycles.Periodization`, `Mesocycles.AimMuscleGroups` and `Microcycles.DurationInDays/TrainingDistribution`, adds `Mesocycles.ProgramId`, `Microcycles.TrainingTemplateId/WeekNumber`, `TrainingSessions.TemplateSessionId`, `Sets.TemplateSetId`, and converts `TemplateSets.ExpectedRIR` into `ExpectedRPE` (`10 - RIR`). FKs pointing to templates are `NO ACTION` (templates are only soft deleted). It fails on purpose if there are mesocycles/microcycles, because they cannot be mapped to a program/template automatically
- **Repositories**: `IProgramRepository` / `ProgramRepository` is the only entry point to the template hierarchy, and `IMacrocycleRepository` / `MacrocycleRepository` the only one to the cycle hierarchy (`GetByMesocycleIdAsync`, `GetByMicrocycleIdAsync`...). Both always load the whole aggregate with split queries
- **EF Core configurations**: located in `Infrastructure/Configurations/`, implementing `IEntityTypeConfiguration<T>`

## 5. Folder structure

```
training/
├── LightWeight.Training.Domain/
│   ├── Aggregates/      # Macrocycle, TrainingSession, Program, Exercise
│   ├── Entities/        # Mesocycle, Microcycle, Set, TrainingTemplate, TemplateSession, TemplateSet
│   ├── Enum/            # MuscleGroups, Periodization, TrainingDistribution, TrainingStage, VolumeLandmarks
│   ├── Events/          # TrainingSessionFinishedDomainEvent
│   ├── Exceptions/
│   ├── Repositories/    # IMacrocycleRepository, ITrainingSessionRepository, IProgramRepository, IExerciseRepository
│   ├── Uow/             # ITrainingUnitOfWork
│   └── ValueObjects/    # AdvanceTrainingTechniques, RepetitionRange
├── LightWeight.Training.Application/
│   ├── Commands/        # Macrocycles, Mesocycles, Microcycles, Programs, TrainingTemplates, TemplateSessions,
│   │                    # TemplateSets, TemplateSuperSets, TrainingSessions (command + validator + handler)
│   ├── Queries/         # current week, cycles detail, programs/templates, session workout, previous performance...
│   ├── Events/          # domain event → integration event (mapper + factory), rebuild handler
│   └── DependencyInjection.cs
├── LightWeight.Training.Infrastructure/
│   ├── Configurations/  # EF Core configurations
│   ├── Migrations/      # FluentMigrator (2026072911xx / 2026072912xx)
│   ├── Persistence/     # TrainingDbContext, UnitOfWork, Repositories/, Seed/
│   └── DependencyInjection.cs
├── LightWeight.Training.Api/   # TrainingModule (endpoints under /api/training)
└── testing/LightWeight.Training.UnitTests/   # Domain/ and Application/
```

## 6. Dependencies

- **Depends on**: `LightWeight.Shared` (AggregateRoot, Entity, ValueObject, IDomainEvent, ICommand, IQuery, IMediator, etc.)
- **Depended by**: Dashboard (through integration events only)
- **Integration events it publishes**: `training.session.finished.v1`
- **Integration events it listens to**: `dashboard.rebuild.requested.v1` (republishes the finished sessions of the user)

## 7. Current state

**Done:** planning (programs, templates, sessions, sets, supersets, soft delete), sequential cycles with the current week in the home, real session registration (focus mode, substitutions, extra sets, supersets, automatic week close, discard), `training.session.finished.v1` for the Dashboard and the answer to its rebuild, API, unit tests.

**Pending:**
- Cycle events (macro/mesocycle started and finished) for the Dashboard filter by macrocycle.
- Global exception handler (errors reach the client as 500).

## 8. Things NOT to do in this module

- Do NOT add business logic in the API layer — only map to Command/Query and send via `IMediator`
- Do NOT create EF Core relationships in more than one `IEntityTypeConfiguration` for the same FK
- Do NOT reference other module's domain types directly — communicate only via integration events
- Do NOT change value object persistence from embedded columns to separate tables without updating both Configuration and Migration
- Do NOT hard delete `TrainingTemplate`, `TemplateSession` or `TemplateSet` (neither `Remove()` on the DbSet nor on the aggregate collections) — use the soft delete methods
- Do NOT add queries that belong in a dedicated read-side module without considering the separation
