# Module: Dashboard

## Responsibility

The aggregation and visualization module. It consumes the integration events published by the other modules and keeps them as **projections**: read models in its own schema, shaped for charts (evolution of body weight, perimeters, skinfolds and, in the future, training volume, calories, NEAT...). It is the only module that **only receives events**: its output is for the user (views and charts), not for other modules.

It does not decide anything: it records what already happened elsewhere. That is why it has **no Domain layer** (no aggregates, no invariants) — only Application, Infrastructure and Api. Its tables can be emptied and rebuilt at any time from the source modules.

## Projections

### BodyMetrics (implemented)

| Projection | Table | Fed by | Content |
|---|---|---|---|
| `WeightProjection` | `dashboard_BodyWeights` | `bodymetrics.weight.{recorded,deleted}.v1` | `MeasuredAt`, `Weight` (kg) |
| `PerimetersProjection` | `dashboard_BodyPerimeters` | `bodymetrics.perimeters.{recorded,deleted}.v1` | `MeasuredAt`, the 14 perimeters (cm, null if not measured) |
| `SkinFoldsProjection` | `dashboard_BodySkinFolds` | `bodymetrics.skinfolds.{recorded,deleted}.v1` | `MeasuredAt`, the 6 skinfolds (mm) and `Summatory` (null without the six) |

All of them are keyed by the `MeasurementId` of bodymetrics and indexed by `(UserId, MeasuredAt)`, which is how the charts read them.

### Training (implemented)

| Projection | Table | Fed by | Content |
|---|---|---|---|
| `TrainingSessionProjection` | `dashboard_TrainingSessions` | `training.session.finished.v1` | Cycle ids (`MacrocycleId`, `MesocycleId`, `MicrocycleId`), `WeekNumber`, `Name`, `StartedAt`, `FinishedAt`, and the wellbeing it started with (`Motivation`, `Sleep`, `Doms`, 1-10) |
| `TrainingSetProjection` | `dashboard_TrainingSets` | `training.session.finished.v1` | Each set: `Order`, exercise, `Repetitions`, `Weight`, `Rpe`, `IsBodyWeight`, `Technique`, `IsPlanned`, `ExerciseMuscleGroups` (text[]) and, from its planned set, `PlannedMuscleGroups` (text[]) and `PlannedMinReps`/`PlannedMaxReps` (null for an extra set) |

Keyed by the ids of training; sessions indexed by `(UserId, StartedAt)`, sets by `(UserId, ExerciseId)` and `SessionId`. Storing the cycle ids makes the future macrocycle/mesocycle filter a plain `WHERE`.

## Calculations

| Calculation | Where | Rule |
|---|---|---|
| Weekly average of the weight | `Application/Calculations/WeeklyAverages.cs` | ISO weeks (Monday 00:00 to Sunday, UTC); average of the weigh-ins of the week rounded to 2 decimals; weeks without weigh-ins do not appear |
| Estimated 1RM | `Calculations/Training/OneRepMax.cs` | Epley counting the repetitions in reserve (RIR = 10 − RPE): `weight × (1 + (reps + RIR) / 30)`. E.g. 100 kg × 10 @ RPE 8 → 140 kg. Not estimated for body weight sets, sets with a technique (drop set, cluster, myo-rep), without load, or with reps + RIR over **30**; flagged **less precise** over **15** |
| Repetition zone | `Calculations/Training/RepZones.cs` | Of an exercise in a session, from the **planned range of its first set** (its middle: 8–10 → 9); an extra set uses the repetitions performed. **Strength ≤ 6, hypertrophy 7–15, endurance ≥ 16** |
| Weekly volume | `Calculations/Training/WeeklyVolume.cs` | Per training week (microcycle, drawn at its first session). Each set adds 1 to every muscle group of its **planned set**; an extra set, to the ones of the **exercise** |
| Fatigue between sets | `Calculations/Training/SetFatigue.cs` | Per exercise and session, with the sets that can be estimated in their order: `(1RM₁ − 1RMₙ) / 1RM₁ × 100` from the first to the second and to the last set (negative if it improved). Needs two sets; labelled with its zone so efforts are never mixed |
| Session performance | `Calculations/Training/SessionPerformance.cs` | For each exercise: estimated 1RM of its **first** set (the freshest: how the user arrived that day) against the first set of the **last time in the same zone**, in %. The session's performance is the **average**; exercises without a previous time don't count, and a change over **±50 %** is left out as implausible (almost always a registration error or a weighted/assisted exercise registered with the added load only) and **reported** in the response (`excluded`) so the user can check it. Comparing with the previous time keeps the normal progression from hiding a bad day |
| Trend of an exercise | `ExerciseHistory.Trend` | Best estimated 1RM of the last session against the previous session in the same zone, in % |

The training calculations run **when reading** from the projections (pure functions with unit tests): changing a rule is a code change, no "Rebuild" needed.

Calculations are done **when reading**, not stored as projections: the volumes are small and there is nothing more to keep in sync. They are pure functions in Application instead of a Domain layer, because they protect no invariant; they will move to a Domain project if calculations with rules of their own appear (fat percentage, energy balance).

## Events Subscribed To

| Integration event | Origin | Action |
|---|---|---|
| `bodymetrics.weight.recorded.v1` | BodyMetrics | Upsert the weight by `MeasurementId` |
| `bodymetrics.weight.deleted.v1` | BodyMetrics | Delete the weight (nothing if it does not exist) |
| `bodymetrics.perimeters.recorded.v1` | BodyMetrics | Upsert the perimeters |
| `bodymetrics.perimeters.deleted.v1` | BodyMetrics | Delete the perimeters |
| `bodymetrics.skinfolds.recorded.v1` | BodyMetrics | Upsert the skinfolds and their summatory |
| `bodymetrics.skinfolds.deleted.v1` | BodyMetrics | Delete the skinfolds |
| `training.session.finished.v1` | Training | Upsert the session and replace its sets (no duplicates if it arrives twice) |

`…recorded` arrives when a measurement is registered, when it is corrected and when it is published again during a rebuild, so the projectors always **upsert**. Applying the same event twice leaves the same result.

## Events Emitted

| Integration event | Name | Payload | Raised when |
|---|---|---|---|
| `RebuildRequestedIntegrationEvent` | `dashboard.rebuild.requested.v1` | `UserId`, `OccurredAtUtc` | The user asks to rebuild their projections |

It is not a notification of something that happened in the dashboard, but a request to the source modules. Contract: `shared/Contracts/Dashboard/DashboardIntegrationEvents.cs`.

## Rebuild

Events live in memory during the request that raises them: they are **not stored anywhere** (by design there is no outbox nor event store, see Open Notes), so they cannot be replayed. To rebuild, the sources regenerate them from their current state:

1. `POST /api/dashboard/rebuild` → `RebuildProjectionsCommand(UserId)`.
2. The dashboard **empties every projection of the user** (this also removes the measurements whose `…deleted` event it missed).
3. It publishes `dashboard.rebuild.requested.v1`.
4. Each subscribed source module (today, BodyMetrics) publishes a `…recorded` event for every row of that user.
5. The same projectors fill the tables again.

The dashboard does not know which modules answer: a new source only has to subscribe to the event. Rebuilding twice gives the same result, and other users are never touched.

## API

All endpoints require authentication and work on the projections of the authenticated user (`/api/dashboard`).

| Method | Route | Body | Response |
|--------|-------|------|----------|
| `POST` | `/rebuild` | — | 204 — projections of the user rebuilt from the sources |
| `GET` | `/bodymetrics/weight` | — | `{ points: [{ measuredAt, weight }], weeklyAverages: [{ weekStart, average, count }] }` — whole history, from the oldest |
| `GET` | `/bodymetrics/perimeters` | — | `[{ measuredAt, neck, …, leftCalf }]` — whole history, null if not measured |
| `GET` | `/bodymetrics/skinfolds` | — | `[{ measuredAt, abdominal, …, calf, summatory }]` — whole history, summatory only with the six |
| `GET` | `/training/volume` | — | `[{ microcycleId, weekNumber, startedAt, setsByMuscle: { "Chest": 12, … } }]` |
| `GET` | `/training/sessions` | — | `[{ sessionId, name, startedAt, performance (% or null), comparedExercises, motivation, sleep, doms }]` |
| `GET` | `/training/exercises` | — | `[{ exerciseId, name, lastPerformedAt, sessions, lastEstimatedOneRepMax, trend }]`, from the most recently trained |
| `GET` | `/training/exercises/{exerciseId}` | — | `{ name, progression[…], fatigue[{ performedAt, zone, firstToSecond, firstToLast }], recentSessions[…] }`; **404** if the user never performed it (same answer for an id that does not exist, so other users' data cannot be probed) |

## Frontend

The charts are grouped by area: body measurements (and, in the future, nutrition) on one side, training on another. The body measurement charts are the **"Evolución" tab of `/bodymetrics`**, built with Chart.js through ng2-charts:

* **Peso**: each weigh-in as a point and the weekly average as a line (drawn in the middle of its week).
* **Perímetros**: the perimeters chosen with a selector (waist by default, up to 4 at once).
* **Sumatorio de pliegues**: only the measurements with the six skinfolds.
* **Pliegues**: the skinfolds chosen with a selector (up to 4). Separate from the summatory because their scales are very different (one axis per chart).
* **Recalcular**: calls `POST /rebuild` and reloads.

Training charts:

* **Home** (below the current week):
  * **Session performance**: lollipops around 0 (blue better, red worse, with the sign in the label; a dot keeps 0 % visible; the axis covers at least ±5 % and is clipped at ±25 %, where an arrow marks a value out of scale with its real value in the tooltip; excluded exercises appear in the tooltip), stacked over **sleep, DOMS and motivation** on the same date axis, so a bad session and a bad night fall on the same vertical.
  * **Weekly volume** per muscle group (chips, up to 4).
  * **Recalcular**: the first time, it loads the history of sessions finished before the event existed.
* **Progreso** (`/progress`, in the Entrenamiento menu):
  * The list of exercises: search, last session, current estimated 1RM, trend with arrow and number.
  * **`/progress/:exerciseId`**: estimated 1RM and best weight (hollow points = less precise; the tooltip shows the wellbeing of the session), performance lost between sets by zone (1ª→2ª / 1ª→última), and the last 10 sessions as a table.

## Open Notes

* **Projections can get out of sync, and that is accepted by design.** The projectors run synchronously inside the request of the source module, after its commit. If a projector fails, the measurement is saved but the projection misses it. In a modular monolith everything runs in the same process, so such a failure means a bug or an unhandled edge case: it is diagnosed with logs and repaired with "Rebuild". An outbox was evaluated and **discarded** (2026-10-16); do not add one without revisiting this decision.
* **Pending (event-flow logs):** structured logs when dispatching and publishing events (event, handler, ids, duration, exception). When it is done, a failing subscriber of another module will be logged as an error instead of returning a 500, because the user's data was saved; "Rebuild" repairs the projection.
* **The rebuild is not atomic.** Emptying and refilling are separate writes; if it fails halfway, running it again leaves everything right.
* **Range:** the charts show the whole history. Training projections already store their cycle ids, so the macrocycle/mesocycle filter will be an optional `?macrocycleId=`/`?mesocycleId=` on the training endpoints, with no change in training. Training will publish its cycle start/finish events for the selector and for the body measurement charts, where a cycle translates to its date range. Pending decision: whether, with a cycle filtered, the trend and the “previous time” only compare inside that cycle.
* **Fat percentage:** needs the sex of the user, which would come from a UserProfile event.
* **Future sources:** Nutrition (FatSecret data: calories and macros) and NEAT. Each one will publish its integration events and subscribe to `dashboard.rebuild.requested.v1`.
