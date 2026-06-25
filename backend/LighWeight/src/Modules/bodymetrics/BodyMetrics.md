# Module: Dashboard

## Responsibility
This is the aggregation and visualization module: it consumes domain events published by the other modules (**BodyMetrics, NEAT, DeviceIntegration, Nutrition, Training**, and potentially **Auth/UserProfile**) and transforms them into data optimized for graphing. According to the diagram, it is the only module that only receives events—it does not emit its own domain events, as its output is user-facing (views/graphs) rather than directed toward other modules.

Conceptually, it acts as a dedicated *read model*, independent of the original aggregates of each module, and is optimized for queries and visualization rather than transactional consistency.

## Main Entities / Aggregates
* **Dashboard (Root Aggregate / Projection):** * *Entity* — A generic marker in the diagram, pending final specification. It will most likely represent the different read projections (one per graph/widget type: weight evolution, training volume, calories, NEAT activity, etc.).

## Events Emitted
* **None.** It is a terminal module in the event flow—its job is to transform and serve data, not to notify other modules.

## Events Subscribed To

| Origin Event | Origin Module | Action Upon Receipt |
| :--- | :--- | :--- |
| `BodyMetrics.WeightMeasureRecorded` | BodyMetrics | Update the body weight evolution projection. |
| `BodyMetrics.PerimetrosRecorded` | BodyMetrics | Update the perimeter evolution projection. |
| `BodyMetrics.PliegueRecorded` | BodyMetrics | Update the estimated body fat percentage evolution projection. |
| `Neat.ActivityRegistered` | NEAT | Update the caloric expenditure projection for extra activity. |
| `DeviceIntegration.ScaleDataReceived` | DeviceIntegration | *(Optional)* If raw scale data needs to be graphed in addition to the consolidated data from BodyMetrics. |
| `DeviceIntegration.AmazFitDataReceived` | DeviceIntegration | Update steps, activity, and heart rate projections. |
| `Nutrition.DiaryDayUpdated` / `DiaryDayClosed` | Nutrition | Update the daily calorie/macronutrient projection. |
| `Training.TrainingDayCompleted` | Training | Update the training adherence and volume projection. |
| `Training.SerieRecorded` | Training | Update the load progression projection per exercise. |
| `Training.MesocicloStarted` / `MesocicloCompleted` | Training | Mark the start/end of blocks on the graph timelines. |

## Open Notes
* **Specify the exact nature of the "Entity" in the diagram:** It will most likely turn into several concrete projections (`WeightTrendView`, `TrainingVolumeView`, `CalorieBalanceView`, etc.) instead of a single generic entity.
* **Decide on the projection strategy:** Should materialized projections be stored in their own schema (`dashboard`) and updated via event handlers, or should they be calculated on the fly by querying other modules? Given that this is a modular monolith (not distributed microservices), evaluate whether materialization is truly necessary or if well-indexed cross-module queries would suffice.
* **Define replay needs:** Determine if it needs to replay historical events for users joining with existing data, or if it will only consume events from the moment of its creation onward.
* **Decide on cron/refresh intervals:** Determine if any projection requires heavy calculations (weekly/monthly aggregates) and should be updated on a schedule rather than event-by-event.