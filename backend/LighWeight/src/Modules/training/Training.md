# Module: Training

## Responsibility

This is the core module for planning and logging strength/bodybuilding training sessions. It models the complete sports planning hierarchy: `Macrocycle` (long block, months) → `Mesocycle` (weeks) → `Microcycle` (training week) → `TrainingSession` (a single session) → `Set` (each performed set), supported by an `Exercise` catalog and `TrainingTemplate` for reusable session blueprints.

Given the focus on bodybuilding (load progression, volume, periodization), this module will likely hold the most business logic in the entire application.

## Domain Model

### Aggregates (all extend `AggregateRoot<Guid>`)

| Aggregate | Properties | Child entities | DB Table |
|-----------|-----------|----------------|----------|
| `Macrocycle` | `UserId`, `StartAt`, `EndAt?`, `Stage` (enum), `Periodization` (enum), `Comments?` | None | `training_Macrocycles` |
| `Mesocycle` | `MacrocycleId`, `AimMuscleGroups` (jsonb), `MotivationLevel`, `Injuries?`, `Comments?`, `StartAt`, `EndAt` | None | `training_Mesocycles` |
| `Microcycle` | `MesocycleId`, `DurationInDays`, `TrainingDistribution` (enum) | None | `training_Microcycles` |
| `TrainingSession` | `MicrocycleId`, `Name`, `StartAt`, `Duration` (interval), `Comments?`, `MotivationLevel`, `SleepLevel`, `DOMSLevel` | `Set` | `training_TrainingSessions` |
| `Program` | `UserId`, `Name`, `Periodization` (enum), `AimMuscleGroups` (jsonb) | `TrainingTemplate` | `training_Programs` |
| `Exercise` | `Name`, `IsBilateral`, `AimMuscleGroups` (jsonb) | None | `training_Exercises` |

### Entities (child of aggregates)

| Entity | Parent | Properties | DB Table |
|--------|--------|-----------|----------|
| `Set` | `TrainingSession` | `ExerciseId`, `Repetitions`, `IsBodyWeight`, `AdvanceTrainingTechniques` (owned), `Weight`, `RPE`, `SuperSetGroupId?` | `training_Sets` |
| `TrainingTemplate` | `Program` | `TrainingDistribution` (enum), `VolumeLandmark` (enum), `DurationInDays`, `Order`, `IsDeleted`, `DeletedAt?` | `training_TrainingTemplates` |
| `TemplateSession` | `TrainingTemplate` | `Name`, `IsDeleted`, `DeletedAt?` | `training_TemplateSessions` |
| `TemplateSet` | `TemplateSession` | `ExerciseId`, `RepetitionRange` (owned), `ExpectedRPE`, `AdvanceTrainingTechniques` (owned), `SuperSetGroupId?`, `AimMuscleGroups`, `IsDeleted`, `DeletedAt?` | `training_TemplateSets` |

### Value Objects

| Value Object | Properties | Used by |
|-------------|-----------|---------|
| `AdvanceTrainingTechniques` | `IsDropSet`, `IsCluster`, `IsMyoRep` (at most one true) | `Set`, `TemplateSet` |
| `RepetitionRange` | `Min`, `Max` (swapped if max < min) | `TemplateSet` |

### Enums

| Enum | Values |
|------|--------|
| `TrainingStage` | `Bulk`, `Cut`, `Maintenance` |
| `Periodization` | `Linear`, `Ondulating`, `block` |
| `TrainingDistribution` | `PushPullLegs`, `UpperLower`, `Weider`, `Phat`, `FullBody`, `Other` |
| `MuscleGroups` | `Shoulder`, `Back`, `Chest`, `Biceps`, `Triceps`, `Glutes`, `Quads`, `Hamstring`, `calves` |

## Hierarchy

```
Macrocycle  (months-long block, e.g. "2026 Bulk")
└── Mesocycle  (weeks-long phase, specific goal)
    └── Microcycle  (training week with split distribution)
        └── TrainingSession  (a single workout session)
            └── Set  (each performed set with weight/reps/RPE)

Program  (aggregate root of the planning side)
└── TrainingTemplate  (reusable blueprint, soft deletable)
    └── TemplateSession  (planned session within a template, soft deletable)
        └── TemplateSet  (planned set with rep range and RPE target, soft deletable)
```

## Application Layer

| Command | Handler | Validator |
|---------|---------|-----------|
| `CreateMacrocycleCommand` | ✅ | ✅ |
| `CreateMesocycleCommand` | ✅ | ✅ |
| `CreateMicrocycleCommand` | ✅ | ✅ |
| `CreateProgramCommand` | ✅ | ✅ |
| `CreateTrainingSessionCommand` | ✅ | ✅ |
| `CreateTrainingTemplateCommand` | ✅ | ✅ |

| Query | Handler |
|-------|---------|
| `GetCurrentMacrocycleQuery` | ❌ (throws `NotImplementedException`) |

## Infrastructure Layer

- **DbContext**: `TrainingDbContext` with `modelBuilder.HasDefaultSchema("training")` and `ApplyConfigurationsFromAssembly`
- **Unit of Work**: `ITrainingUnitOfWork` / `UnitOfWork` (dispatches domain events after save)
- **EF Core Configurations**: All 7 `IEntityTypeConfiguration<T>` files in `Configurations/`
- **Migrations**: 7 FluentMigrator files in `Migrations/` (all tables in `training` schema)
- **Soft delete**: global query filter `!IsDeleted` on `TrainingTemplate`, `TemplateSession` and `TemplateSet` (migration `202607291213_AddSoftDeleteToTemplates`)
- **Program restructure**: migration `202607291214_RestructureTrainingAroundProgram`; `ProgramRepository` loads the whole aggregate
- **Pending**: Concrete repository implementations, `GetCurrentMacrocycleQueryHandler`

## Events

No domain events or integration events have been implemented yet. The `AggregateRoot<Guid>` base class provides `RaiseDomainEvent()` / `ClearDomainEvents()` support, ready for future use.

## Key Design Decisions

- Exercises are user-managed (each user creates their own catalog)
- Supersets use `SuperSetGroupId` (nullable GUID), not a boolean flag
- `AdvanceTrainingTechniques` enforces at most one technique per set via domain validation
- `EndTraining()` calculates duration using `TimeOnly` subtraction (known bug: incorrect across midnight)
- `RepetitionRange.Create` silently swaps min/max if max < min (doesn't throw)
- Enums stored as strings in the database
- Value objects persisted as embedded columns (via `OwnsOne`)
- `List<MuscleGroups>` collections stored as `jsonb` columns
- Templates (`TrainingTemplate`, `TemplateSession`, `TemplateSet`) use soft delete (`IsDeleted` + `DeletedAt`): `Microcycle`, `TrainingSession` and `Set` keep references to them, so they are never physically deleted. Deletion goes through the parent `Delete...` method, cascades the flag down the hierarchy, and EF Core hides deleted rows with a global query filter (`IgnoreQueryFilters()` to read history)