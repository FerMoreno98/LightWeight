# AGENTS.md — Module: Training

## 1. Module purpose

This module handles strength and bodybuilding training planning and logging. It models the full training hierarchy: `Macrocycle` (long block, months) → `Mesocycle` (weeks) → `Microcycle` (training week) → `TrainingSession` (a single session) → `Set` (each performed set). It also provides `TrainingTemplate` as reusable session blueprints and `Exercise` as a user-managed exercise catalog.

## 2. Aggregates and entities

### `Macrocycle` (AggregateRoot)
- **Properties**: `UserId`, `StartAt`, `EndAt?`, `Stage` (TrainingStage: Bulk/Cut/Maintenance), `Periodization` (Linear/Ondulating/block), `Comments?`
- **Invariants**:
  - Belongs to a single user (`UserId`)
  - Can only be finished once (`EndAt` is set via `Finish()` and should not change afterwards)
  - `Stage` determines whether the user is bulking, cutting, or maintaining
  - `Periodization` defines the load progression scheme (linear, ondulating, block)
- **Child entities**: None
- **Domain events it emits**: None yet
- **References to other modules**: None (only references `UserId` from the Auth module's JWT)
- **DB table**: `training_Macrocycles`

### `Mesocycle` (AggregateRoot)
- **Properties**: `MacrocycleId`, `AimMuscleGroups` (List\<MuscleGroups\>), `MotivationLevel` (1-10), `Injuries?`, `Comments?`, `StartAt`, `EndAt`
- **Invariants**:
  - References a parent `MacrocycleId`
  - `StartAt` must be before `EndAt`
  - `MotivationLevel` is a subjective user rating (1-10)
- **Child entities**: None
- **Domain events it emits**: None yet
- **DB table**: `training_Mesocycles` (FK → Macrocycle, `_aimMuscleGroups` stored as `jsonb`)

### `Microcycle` (AggregateRoot)
- **Properties**: `MesocycleId`, `DurationInDays`, `TrainingDistribution` (PushPullLegs/UpperLower/Weider/Phat/FullBody/Other)
- **Invariants**:
  - References a parent `MesocycleId`
  - `DurationInDays` is typically 7 but can vary
  - `TrainingDistribution` defines the weekly split (Push/Pull/Legs, Upper/Lower, etc.)
- **Child entities**: None
- **Domain events it emits**: None yet
- **DB table**: `training_Microcycles` (FK → Mesocycle)

### `TrainingSession` (AggregateRoot)
- **Properties**: `MicrocycleId`, `Name`, `StartAt`, `Duration` (TimeSpan), `Comments?`, `MotivationLevel` (1-10), `SleepLevel` (1-10), `DOMSLevel` (1-10)
- **Invariants**:
  - References a parent `MicrocycleId`
  - `Duration` starts as zero and is calculated when `EndTraining()` is called
  - `MotivationLevel`, `SleepLevel`, and `DOMSLevel` are subjective user ratings (1-10)
- **Child entities**: `Set`
- **Domain events it emits**: None yet
- **Behaviour methods**:
  - `EndTraining(DateTime now)`: calculates total session duration
  - `RegisterSet(Set set)`: adds a performed set to the session
- **DB table**: `training_TrainingSessions` (FK → Microcycle, `Duration` as `interval`)

### `TrainingTemplate` (AggregateRoot)
- **Properties**: `UserId`, `Name`, `TrainingDistribution`
- **Invariants**:
  - Belongs to a single user (`UserId`)
  - `Name` is user-defined and should be unique per user
  - `TrainingDistribution` mirrors the same enum used by `Microcycle`
- **Child entities**: `TemplateSession`
- **Domain events it emits**: None yet
- **DB table**: `training_TrainingTemplates`

### `Exercise` (AggregateRoot)
- **Properties**: `Name`, `IsBilateral`, `AimMuscleGroups` (List\<MuscleGroups\>)
- **Invariants**:
  - `AimMuscleGroups` lists the muscle groups this exercise can work (not necessarily all are targeted in every set)
  - `IsBilateral` indicates whether the exercise involves both limbs simultaneously
- **Child entities**: None
- **Domain events it emits**: None yet
- **Design note**: Exercises belong to a user-managed catalog. Users can create their own exercises. The `Set` and `TemplateSet` entities reference `ExerciseId` rather than embedding the full value.
- **DB table**: `training_Exercises` (`_aimMuscleGroups` stored as `jsonb`)

### `Set` (Entity, child of `TrainingSession`)
- **Properties**: `ExerciseId`, `Repetitions`, `IsBodyWeight`, `AdvanceTrainingTechniques` (ValueObject), `Weight`, `RPE`, `SuperSetGroupId?`
- `SuperSetGroupId` (nullable): shared ID across sets that form a superset; `null` means not part of a superset
- `AdvanceTrainingTechniques` is a ValueObject (IsDropSet/IsCluster/IsMyoRep) enforcing at most one technique active per set
- **DB table**: `training_Sets` (FK → TrainingSession, FK → Exercise; `AdvanceTrainingTechniques` columns embedded)

### `TemplateSession` (Entity, child of `TrainingTemplate`)
- **Properties**: `Name`
- Represents a planned session within a template
- Contains a collection of `TemplateSet`
- **DB table**: `training_TemplateSessions` (FK → TrainingTemplate)

### `TemplateSet` (Entity, child of `TemplateSession`)
- **Properties**: `ExerciseId`, `RepetitionRange` (ValueObject: Min/Max), `ExpectedRIR`, `AdvanceTrainingTechniques`, `SuperSetGroupId?`
- Uses `RepetitionRange` (min-max) instead of a fixed rep count to allow autoregulation
- **DB table**: `training_TemplateSets` (FK → TemplateSession, FK → Exercise; `RepetitionRange` and `AdvanceTrainingTechniques` columns embedded)

## 3. Non-obvious business rules

- **Supersets are modeled via `SuperSetGroupId`**: There is no `IsSuperSet` boolean. Sets sharing the same `SuperSetGroupId` belong to the same superset. This allows pairs, triplets, or any group size and keeps the model relational without coupling entities.
- **At most one advanced technique per set**: `AdvanceTrainingTechniques` enforces that only one of `IsDropSet`, `IsCluster`, or `IsMyoRep` can be `true`. Attempting to create with more than one active throws `AdvanceTrainingTechniquesExceptions`.
- **Template vs. real sessions**: `TrainingTemplate`, `TemplateSession`, and `TemplateSet` are planning aggregates and never reference real session IDs. Real `TrainingSession` and its `Set` are created independently, possibly copying structure from a template.
- **`EndTraining` duration calculation**: Currently uses `TimeOnly` subtraction, which may produce incorrect results if the session crosses midnight. This is a known bug.
- **`RepetitionRange.Create` silently swaps values**: If `max < min`, the values are swapped instead of throwing. A `DomainException` may be more appropriate for invalid input.

## 4. Database schema

- **Schema**: `training` (PostgreSQL, managed by FluentMigrator and EF Core)
- **Tables**:

| Table | Columns | FK |
|-------|---------|----|
| `training_Macrocycles` | `Id` (guid PK), `UserId`, `StartAt`, `EndAt?`, `Stage` (varchar 20), `Periodization` (varchar 20), `Comments?` | — |
| `training_Mesocycles` | `Id` (guid PK), `MacrocycleId`, `AimMuscleGroups` (jsonb), `MotivationLevel`, `Injuries?`, `Comments?`, `StartAt`, `EndAt` | `MacrocycleId` → Macrocycles (cascade) |
| `training_Microcycles` | `Id` (guid PK), `MesocycleId`, `DurationInDays`, `TrainingDistribution` (varchar 20) | `MesocycleId` → Mesocycles (cascade) |
| `training_TrainingSessions` | `Id` (guid PK), `MicrocycleId`, `Name` (200), `StartAt`, `Duration` (interval), `Comments?`, `MotivationLevel`, `SleepLevel`, `DOMSLevel` | `MicrocycleId` → Microcycles (cascade) |
| `training_Sets` | `Id` (guid PK), `TrainingSessionId`, `ExerciseId`, `Repetitions`, `IsBodyWeight`, `Weight` (decimal 8,2), `RPE` (decimal 3,1), `SuperSetGroupId?`, `IsDropSet`, `IsCluster`, `IsMyoRep` | `TrainingSessionId` → TrainingSessions (cascade), `ExerciseId` → Exercises (cascade) |
| `training_TrainingTemplates` | `Id` (guid PK), `UserId`, `Name` (200), `TrainingDistribution` (varchar 20) | — |
| `training_TemplateSessions` | `Id` (guid PK), `TrainingTemplateId`, `Name` (200) | `TrainingTemplateId` → TrainingTemplates (cascade) |
| `training_TemplateSets` | `Id` (guid PK), `TemplateSessionId`, `ExerciseId`, `ExpectedRIR`, `SuperSetGroupId?`, `RepetitionRange_Min`, `RepetitionRange_Max`, `IsDropSet`, `IsCluster`, `IsMyoRep` | `TemplateSessionId` → TemplateSessions (cascade), `ExerciseId` → Exercises (cascade) |
| `training_Exercises` | `Id` (guid PK), `Name` (200), `IsBilateral`, `AimMuscleGroups` (jsonb) | — |

- **Migrations**: located in `Infrastructure/Migrations/`, naming convention `{yyyyMMddHHmm}_{Description}.cs`
- **EF Core configurations**: located in `Infrastructure/Configurations/`, implementing `IEntityTypeConfiguration<T>`

## 5. Folder structure

```
training/
├── LightWeight.Training.Domain/
│   ├── Aggregates/            # Macrocycle, Mesocycle, Microcycle, TrainingSession, TrainingTemplate, Exercise
│   ├── Entities/              # Set, TemplateSession, TemplateSet
│   ├── Enum/                  # MuscleGroups, Periodization, TrainingDistribution, TrainingStage
│   ├── Events/                # (empty — no domain events defined yet)
│   ├── Exceptions/            # TrainingDomainException, AdvanceTrainingTechniquesExceptions
│   ├── Repositories/          # IMacrocycleRepository (AddAsync), IExerciseRepository (empty)
│   ├── Uow/                   # ITrainingUnitOfWork
│   └── ValueObjects/          # AdvanceTrainingTechniques, RepetitionRange
│
├── LightWeight.Training.Application/
│   ├── Commands/
│   │   ├── Macrocycles/CreateMacrocycle/        # Command + Handler + Validator
│   │   ├── Mesocycles/CreateMesocycle/          # Command + Handler + Validator
│   │   ├── Microcycles/CreateMicrocycle/        # Command + Handler + Validator
│   │   ├── TrainingSessions/CreateTrainingSession/  # Command + Handler + Validator
│   │   └── TrainingTemplates/CreateTrainingTemplate/ # Command + Handler + Validator
│   ├── Events/                # (empty — no event handlers defined yet)
│   ├── Exceptions/            # (empty — no application exceptions defined yet)
│   └── Queries/
│       └── Macrocycles/GetCurrentMacrocycle/    # Query + Handler (throws NotImplementedException)
│
├── LightWeight.Training.Infrastructure/
│   ├── Configurations/        # Macrocycle, Mesocycle, Microcycle, Exercise,
│   │                          # TrainingTemplate, TemplateSession, TemplateSet,
│   │                          # TrainingSession, Set (IEntityTypeConfiguration)
│   ├── Migrations/            # 7 FluentMigrator migrations (all tables in `training` schema)
│   ├── Persistence/
│   │   ├── TrainingDbContext.cs     # EF Core DbContext (applies configs via assembly scanning)
│   │   └── UnitOfWork.cs           # ITrainingUnitOfWork implementation
│   └── DependencyInjection.cs      # Infrastructure service registration
│
├── LightWeight.Training.Api/
│   ├── DTOs/                  # (empty — no DTOs defined yet)
│   └── TrainingModule.cs      # (not created yet)
│
└── testing/
    └── LightWeight.Training.UnitTests/
        ├── Domain/            # (empty — no domain tests yet)
        └── Application/       # (empty — no handler tests yet)
```

## 6. Dependencies

- **Depends on**: `LightWeight.Shared` (AggregateRoot, Entity, ValueObject, IDomainEvent, ICommand, IQuery, IMediator, etc.)
- **Depended by**: None yet
- **Integration events it publishes**: None yet
- **Integration events it listens to**: None yet

## 7. Current state

**Done:**
- Domain aggregates and entities with factory methods and invariants
- Application commands + handlers + validators for all CRUD operations
- EF Core configurations for all 9 tables (value objects as owned columns, enum collections as jsonb)
- FluentMigrator migrations for all 9 tables in `training` schema
- TrainingDbContext with assembly-scanning for configurations
- ITrainingUnitOfWork + UnitOfWork implementation

**Pending / in progress:**
- Repository implementations (concrete classes for IMacrocycleRepository, IExerciseRepository)
- GetCurrentMacrocycleQueryHandler (throws NotImplementedException)
- IExerciseRepository is an empty interface (no methods defined)
- API layer (endpoints, DTOs, TrainingModule.cs)
- Domain events
- Unit tests

## 8. Things NOT to do in this module

- Do NOT add business logic in the API layer — only map to Command/Query and send via `IMediator`
- Do NOT create EF Core relationships in more than one `IEntityTypeConfiguration` for the same FK
- Do NOT reference other module's domain types directly — communicate only via integration events
- Do NOT change value object persistence from embedded columns to separate tables without updating both Configuration and Migration
- Do NOT add queries that belong in a dedicated read-side module without considering the separation
