# Module: Training

## Responsibility

The core module for planning and logging strength/bodybuilding training. It has two sides:

- **Planning:** a `Program` with reusable `TrainingTemplate`s (weeks), each with `TemplateSession`s and planned `TemplateSet`s (exercise, repetition range, expected RPE, muscle groups, technique, superset).
- **Execution:** the cycle hierarchy `Macrocycle` → `Mesocycle` → `Microcycle` (training week, following a template), and the `TrainingSession`s actually performed, with their real `Set`s.

The analysis of the history (volume, progression, fatigue, performance against wellbeing) is **not** done here: it belongs to the Dashboard, which receives every finished session through an integration event.

## Domain Model

### Aggregates (all extend `AggregateRoot<Guid>`)

| Aggregate | Properties | Child entities | DB Table |
|-----------|-----------|----------------|----------|
| `Macrocycle` | `UserId`, `StartedAt`, `FinishedAt?`, `Stage` (enum), `Comments?` | `Mesocycle` → `Microcycle` | `training_Macrocycles` |
| `TrainingSession` | `MicrocycleId`, `UserId`, `TemplateSessionId?`, `Name`, `StartAt`, `FinishedAt?`, `Duration`, `Comments?`, `MotivationLevel`, `SleepLevel`, `DOMSLevel` (1-10) | `Set` | `training_TrainingSessions` |
| `Program` | `UserId`, `Name`, `Periodization` (enum), `AimMuscleGroups` (jsonb) | `TrainingTemplate` → `TemplateSession` → `TemplateSet` | `training_Programs` |
| `Exercise` | `Name`, `IsBilateral`, `AimMuscleGroups` (jsonb) — global catalog, seeded | None | `training_Exercises` |

### Entities (child of aggregates)

| Entity | Parent | Properties | DB Table |
|--------|--------|-----------|----------|
| `Mesocycle` | `Macrocycle` | `ProgramId`, `MotivationLevel`, `Injuries?`, `Comments?`, `StartedAt`, `FinishedAt?` | `training_Mesocycles` |
| `Microcycle` | `Mesocycle` | `TrainingTemplateId`, `WeekNumber`, `StartedAt`, `FinishedAt?` | `training_Microcycles` |
| `Set` | `TrainingSession` | `ExerciseId` (performed, may be a substitute), `TemplateSetId?` (null = extra set), `Repetitions`, `Weight`, `RPE`, `IsBodyWeight`, `AdvanceTrainingTechniques`, `SuperSetGroupId?` | `training_Sets` |
| `TrainingTemplate` | `Program` | `Name`, `TrainingDistribution`, `VolumeLandmark`, `DurationInDays`, `Order`, `IsDeleted`, `DeletedAt?` | `training_TrainingTemplates` |
| `TemplateSession` | `TrainingTemplate` | `Name`, `IsDeleted`, `DeletedAt?` | `training_TemplateSessions` |
| `TemplateSet` | `TemplateSession` | `ExerciseId`, `RepetitionRange`, `ExpectedRPE`, `AdvanceTrainingTechniques?`, `SuperSetGroupId?`, `AimMuscleGroups`, `IsDeleted`, `DeletedAt?` | `training_TemplateSets` |

### Value Objects

| Value Object | Properties | Used by |
|-------------|-----------|---------|
| `AdvanceTrainingTechniques` | `IsDropSet`, `IsCluster`, `IsMyoRep` (at most one true) | `Set`, `TemplateSet` |
| `RepetitionRange` | `Min`, `Max` (swapped if max < min) | `TemplateSet` |

### Enums

| Enum | Values |
|------|--------|
| `TrainingStage` | `Bulk`, `Cut`, `Maintenance` |
| `Periodization` | `Linear`, `Ondulating`, `block`, `MikeIsraetel` |
| `TrainingDistribution` | `PushPullLegs`, `UpperLower`, `Weider`, `Phat`, `FullBody`, `Other` |
| `VolumeLandmarks` | `MV`, `MEV`, `MAV`, `MRV` |
| `MuscleGroups` | `Shoulder`, `Back`, `Chest`, `Biceps`, `Triceps`, `Glutes`, `Quads`, `Hamstring`, `calves` |

## Hierarchy

```
Macrocycle  (months-long block, e.g. "2026 Bulk"; at most one active per user)
└── Mesocycle  (follows a Program; at most one active)
    └── Microcycle  (training week following a TrainingTemplate; at most one active, numbered 1, 2, 3...)
        └── TrainingSession  (a session of the week's template; read only once finished)
            └── Set  (each performed set: weight, reps, RPE)

Program  (planning side)
└── TrainingTemplate  (a week blueprint, soft deletable)
    └── TemplateSession  (planned session, soft deletable)
        └── TemplateSet  (planned set: rep range, expected RPE, muscle groups; soft deletable)
```

## Events Emitted

| Integration event | Name | Payload | Raised when |
|---|---|---|---|
| `TrainingSessionFinishedIntegrationEvent` | `training.session.finished.v1` | Session: `SessionId`, `UserId`, `MacrocycleId`, `MesocycleId`, `MicrocycleId`, `WeekNumber`, `Name`, `StartedAt`, `FinishedAt`, `Motivation`, `Sleep`, `Doms`. Each set (`PerformedSetSnapshot`): `SetId`, `Order`, `ExerciseId`, `ExerciseName`, `Repetitions`, `Weight`, `Rpe`, `IsBodyWeight`, `Technique`, `IsPlanned`, `ExerciseMuscleGroups`, `PlannedMuscleGroups?`, `PlannedMinReps?`, `PlannedMaxReps?` | A session is finished |

- **One event per session, with everything inside.** A finished session cannot be edited and a session can only be discarded while in progress, so no "corrected" or "deleted" events are needed.
- **Facts, not interpretations.** Each set carries what was performed *and* what had been planned for it (muscle groups and repetition range of its planned set, null for an extra set). How to count volume or group by effort is decided by the consumer.
- **The planned data comes from the planned set even if it was deleted from the template afterwards** (`IProgramRepository.GetPlannedSetsIncludingDeletedAsync`, skipping the soft delete filter).
- **`Order`** is the position of the set in the session, in the order it was registered (Guid v7 ids); the event makes it explicit so consumers don't depend on how ids are generated.

Flow: `TrainingSession.Finish` raises `TrainingSessionFinishedDomainEvent` → the UnitOfWork saves and dispatches it → `TrainingSessionDomainEventsHandler` builds the contract with `TrainingSessionEventFactory` (loads exercises and planned sets in one query each) and `TrainingIntegrationEventMapper` → `IEventPublisher`.

Contract: `shared/Contracts/Training/TrainingIntegrationEvents.cs`, name in `EventNames.Training`.

## Events Subscribed To

| Integration event | Name | Origin | Action |
|---|---|---|---|
| `RebuildRequestedIntegrationEvent` | `dashboard.rebuild.requested.v1` | Dashboard | Publishes again a `training.session.finished.v1` for **every finished session** of the user, built from the current tables (`ITrainingSessionRepository.GetFinishedOfAUserAsync`) |

Sessions finished before the event existed reach the dashboard this way: after deploying, "Recalcular" loads the whole history once.

## Key Design Decisions

- Exercises are a global catalog (seeded), not per user.
- Supersets use `SuperSetGroupId` (nullable GUID), not a boolean flag.
- `AdvanceTrainingTechniques` enforces at most one technique per set.
- `RepetitionRange.Create` silently swaps min/max if max < min (doesn't throw).
- Templates use soft delete (`IsDeleted` + `DeletedAt`): `Microcycle`, `TrainingSession` and `Set` keep references to them. EF Core hides deleted rows with a global query filter (`IgnoreQueryFilters()` to read history).
- Cycles are sequential: a new week/mesocycle/macrocycle can only start when the previous one is finished. A week closes by hand, automatically when every planned session is finished, or in cascade when its mesocycle/macrocycle finishes.
