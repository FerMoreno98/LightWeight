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

## Calculations

| Calculation | Where | Rule |
|---|---|---|
| Weekly average of the weight | `Application/Calculations/WeeklyAverages.cs` | ISO weeks (Monday 00:00 to Sunday, UTC); average of the weigh-ins of the week rounded to 2 decimals; weeks without weigh-ins do not appear |

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

`…recorded` arrives when a measurement is registered, when it is corrected and when it is published again during a rebuild, so the projectors always **upsert**. Applying the same event twice leaves the same result.

## Events Emitted

| Integration event | Name | Payload | Raised when |
|---|---|---|---|
| `RebuildRequestedIntegrationEvent` | `dashboard.rebuild.requested.v1` | `UserId`, `OccurredAtUtc` | The user asks to rebuild their projections |

It is not a notification of something that happened in the dashboard, but a request to the source modules. Contract: `shared/Contracts/Dashboard/DashboardIntegrationEvents.cs`.

## Rebuild

Events live in memory during the request that raises them: they are **not stored anywhere** (there is no outbox or event store), so they cannot be replayed. To rebuild, the sources regenerate them from their current state:

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

## Frontend

The charts are grouped by area: body measurements (and, in the future, nutrition) on one side, training on another. The body measurement charts are the **"Evolución" tab of `/bodymetrics`**, built with Chart.js through ng2-charts:

* **Peso**: each weigh-in as a point and the weekly average as a line (drawn in the middle of its week).
* **Perímetros**: the perimeters chosen with a selector (waist by default, up to 4 at once).
* **Sumatorio de pliegues**: only the measurements with the six skinfolds.
* **Pliegues**: the skinfolds chosen with a selector (up to 4). Separate from the summatory because their scales are very different (one axis per chart).
* **Recalcular**: calls `POST /rebuild` and reloads.

## Open Notes

* **Projections can get out of sync.** The projectors run synchronously inside the request of the source module, after its commit. If a projector fails, the measurement is saved but the request returns an error and the projection misses it. "Rebuild" fixes it; an outbox (reliable events, real replay) is the long-term fix and will be done as a separate task in the shared messaging.
* **The rebuild is not atomic.** Emptying and refilling are separate writes; if it fails halfway, running it again leaves everything right.
* **Range:** the charts show the whole history. Filtering by macrocycle will add `?from=&to=` to the read endpoints once Training publishes its events.
* **Fat percentage:** needs the sex of the user, which would come from a UserProfile event.
* **Future sources:** Training (volume, load progression, adherence), Nutrition (FatSecret data: calories and macros) and NEAT. Each one will publish its integration events and subscribe to `dashboard.rebuild.requested.v1`.
