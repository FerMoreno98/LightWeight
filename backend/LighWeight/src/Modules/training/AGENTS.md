# AGENTS.md — Module: Training

## 1. Module purpose

This module handles strength and bodybuilding training planning and logging. It models the full training hierarchy: `Macrocycle` (long block, months) → `Mesocycle` (weeks) → `Microcycle` (training week) → `TrainingSession` (a single session) → `Set` (each performed set). It also provides `TrainingTemplate` as reusable session blueprints and `Exercise` as a user-managed exercise catalog.

## 2. Aggregates and entities

### `Macrocycle` (AggregateRoot)
- **Invariants**:
  - Belongs to a single user (`UserId`)
  - Can only be finished once (`EndAt` is set via `Finish()` and should not change afterwards)
  - `AimMuscleGroups` defines which muscle groups the user prioritises during the block
  - `Stage` determines whether the user is bulking, cutting, or maintaining
  - `Periodization` defines the load progression scheme (linear, ondulating, block)
- **Child entities**: None
- **Domain events it emits**: None yet
- **References to other modules**: None (only references `UserId` from the Auth module's JWT)

### `Mesocycle` (AggregateRoot)
- **Invariants**:
  - References a parent `MacrocycleId`
  - `StartAt` must be before `EndAt`
  - `MotivationLevel` is a subjective user rating (1-10)
- **Child entities**: None
- **Domain events it emits**: None yet

### `Microcycle` (AggregateRoot)
- **Invariants**:
  - References a parent `MesocycleId`
  - `DurationInDays` is typically 7 but can vary
  - `TrainingDistribution` defines the weekly split (Push/Pull/Legs, Upper/Lower, etc.)
- **Child entities**: None
- **Domain events it emits**: None yet

### `TrainingSession` (AggregateRoot)
- **Invariants**:
  - References a parent `MicrocycleId`
  - `Duration` starts as zero and is calculated when `EndTraining()` is called
  - `MotivationLevel`, `SleepLevel`, and `DOMSLevel` are subjective user ratings (1-10)
- **Child entities**: `Set`
- **Domain events it emits**: None yet
- **Behaviour methods**:
  - `EndTraining(DateTime now)`: calculates total session duration
  - `RegisterSet(Set set)`: adds a performed set to the session

### `TrainingTemplate` (AggregateRoot)
- **Invariants**:
  - Belongs to a single user (`UserId`)
  - `Name` is user-defined and should be unique per user
  - `TrainingDistribution` mirrors the same enum used by `Microcycle`
- **Child entities**: `TemplateSession`
- **Domain events it emits**: None yet

### `Exercise` (AggregateRoot)
- **Invariants**:
  - `AimMuscleGroups` lists the muscle groups this exercise can work (not necessarily all are targeted in every set)
  - `IsBilateral` indicates whether the exercise involves both limbs simultaneously
- **Child entities**: None
- **Domain events it emits**: None yet
- **Design note**: Exercises belong to a user-managed catalog. Users can create their own exercises. The `Set` and `TemplateSet` entities reference `ExerciseId` rather than embedding the full value.

### `Set` (Entity, child of `TrainingSession`)
- Represents a performed set within a training session
- Properties: `ExerciseId`, `Repetitions`, `IsBodyWeight`, `AdvanceTrainingTechniques`, `Weight`, `RPE`, `SuperSetGroupId`
- `SuperSetGroupId` (nullable): shared ID across sets that form a superset; `null` means not part of a superset
- `AdvanceTrainingTechniques` is a ValueObject enforcing that at most one technique (drop set, cluster, myo-rep) can be active per set

### `TemplateSession` (Entity, child of `TrainingTemplate`)
- Represents a planned session within a template
- Contains a collection of `TemplateSet`

### `TemplateSet` (Entity, child of `TemplateSession`)
- Represents a planned set within a template session
- Properties: `ExerciseId`, `RepetitionRange`, `ExpectedRIR`, `AdvanceTrainingTechniques`, `SuperSetGroupId`
- Uses `RepetitionRange` (min-max) instead of a fixed rep count to allow autoregulation

## 3. Non-obvious business rules

- **Supersets are modeled via `SuperSetGroupId`**: There is no `IsSuperSet` boolean. Sets sharing the same `SuperSetGroupId` belong to the same superset. This allows pairs, triplets, or any group size and keeps the model relational without coupling entities.
- **At most one advanced technique per set**: `AdvanceTrainingTechniques` enforces that only one of `IsDropSet`, `IsCluster`, or `IsMyoRep` can be `true`. Attempting to create with more than one active throws `AdvanceTrainingTechniquesExceptions`.
- **Template vs. real sessions**: `TrainingTemplate`, `TemplateSession`, and `TemplateSet` are planning aggregates and never reference real session IDs. Real `TrainingSession` and its `Set` are created independently, possibly copying structure from a template.
- **`EndTraining` duration calculation**: Currently uses `TimeOnly` subtraction, which may produce incorrect results if the session crosses midnight. This is a known bug.
- **`RepetitionRange.Create` silently swaps values**: If `max < min`, the values are swapped instead of throwing. A `DomainException` may be more appropriate for invalid input.

## 4. Folder structure

```
training/
├── LightWeight.Training.Domain/
│   ├── Aggregates/            # Macrocycle, Mesocycle, Microcycle, TrainingSession, TrainingTemplate, Exercise
│   ├── Entities/              # Set, TemplateSession, TemplateSet
│   ├── Enum/                  # MuscleGroups, Periodization, TrainingDistribution, TrainingStage
│   ├── Events/                # (empty — no domain events defined yet)
│   ├── Exceptions/            # TrainingDomainException, AdvanceTrainingTechniquesExceptions
│   ├── Repository/            # (empty — no repository interfaces defined yet)
│   ├── Services/              # (empty — no domain services defined yet)
│   ├── Uow/                   # (empty — no unit of work interface defined yet)
│   └── ValueObjects/          # AdvanceTrainingTechniques, RepetitionRange
│
├── LightWeight.Training.Application/
│   ├── Commands/              # (empty — no commands defined yet)
│   ├── Events/                # (empty — no event handlers defined yet)
│   ├── Exceptions/            # (empty — no application exceptions defined yet)
│   └── Queries/               # (empty — no queries defined yet)
│
├── LightWeight.Training.Infrastructure/
│   ├── Persistence/           # (empty — no DbContext, repositories, or migrations yet)
│   ├── Configurations/        # (empty — no EF Core configurations yet)
│   └── Migrations/            # (empty — no FluentMigrator migrations yet)
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
