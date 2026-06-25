# Module: Training

## Responsibility

This is the core module for planning and logging strength/bodybuilding training sessions. It models the complete sports planning hierarchy: `Mesociclo` (long block, weeks/months) → `Microciclo` (training week within the mesocycle) → `TrainingDay` (training session for a specific day) → `Serie` (each set performed within an exercise during that session), supported by an `Exercise` catalog.

Given the focus on bodybuilding (load progression, volume, periodization), this module will likely hold the most business logic in the entire application.

## Main Entities / Aggregates

- **Training** (root aggregate, likely representing the user's active training plan).
  - `Mesociclo` — a planning block spanning several weeks with a specific goal (strength, hypertrophy, cutting...).
  - `Microciclo` — a training week within a mesocycle.
  - `TrainingDay` — a specific training session within a microciclo (a day in the plan).
  - `Serie` — the log of a performed set (reps, weight, RPE/RIR) within an exercise of a `TrainingDay`.
  - `Exercise` (referenced as a catalog, potentially a shared entity/global catalog) — defines the exercise (name, muscle group, equipment).

## Events Emitted

| Event | Tentative Payload | When It Fires |
|---|---|---|
| `Training.MesocicloStarted` | `UserId`, `MesocicloId`, `Objective`, `StartDate`, `EndDate` | Upon starting a new mesocycle. |
| `Training.MesocicloCompleted` | `UserId`, `MesocicloId`, `CompletedAt` | Upon finishing a mesocycle. |
| `Training.TrainingDayCompleted` | `UserId`, `TrainingDayId`, `MesocicloId`, `MicrocicloId`, `CompletedAt`, `TotalVolume` | Upon marking a training session as completed. |
| `Training.SerieRecorded` | `UserId`, `SerieId`, `TrainingDayId`, `ExerciseId`, `Weight`, `Reps`, `RPE?`, `RecordedAt` | Upon logging each performed set (could be very frequent; evaluate whether aggregating it at the `TrainingDayCompleted` level is preferred over emitting per set). |

Mainly consumed by `Dashboard` (load progression, volume per muscle group, plan adherence), and potentially by `Nutrition` (to adjust calories according to the current mesocycle phase).

## Events Subscribed To

According to the diagram, **Training does not consume events from other modules** — it is driven directly by user interaction (training planning and logging).

## Open Notes

- Decide whether `SerieRecorded` is emitted for each individual set (fine granularity, useful for detailed graphs) or if a summary is only emitted upon completing the `TrainingDay` (less event noise).
- `Exercise`: define whether it is a global catalog shared among all users (managed as seed/admin data) or if each user can create their own custom exercises.
- Define the progression/autoregulation model: does the system suggest loads for the next session based on historical data, or is it purely manual user logging?
- Clarify the relationship between `Mesociclo` and `Nutrition` goals (is an integration planned, or do they remain decoupled for now?).