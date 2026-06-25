# Module: DeviceIntegration

## Responsibility

This is the integration module for external applications and devices: smart scales, smartwatches/wearables (AmazFit), and nutritional tracking apps (FatSecret). It is responsible for calling external APIs (likely triggered by `Cron jobs`), normalizing the received data, and publishing them as domain events for the rest of the modules to consume.

According to the diagram, **it is a purely emitting module**: it does not expose its own business logic beyond retrieving and translating external data into internal events.

## Main Entities / Aggregates

- **DeviceIntegration** (root aggregate / integration orchestrator).
  - `AmazFitData` — data coming from the AmazFit wearable (steps, heart rate, sleep, active calories, etc. — to be specified).
  - `ScaleData` — data coming from the smart scale (weight, possibly body fat %, water % depending on the model).
  - `FatSecretData` — data coming from the FatSecret app (external nutritional log/food diary).

## Events Emitted

| Event | Tentative Payload | When It Fires |
|---|---|---|
| `DeviceIntegration.ScaleDataReceived` | `UserId`, `WeightKg`, `BodyFatPercentage?`, `RecordedAt`, `DeviceSource` | Following the cron job upon detecting a new scale reading. |
| `DeviceIntegration.AmazFitDataReceived` | `UserId`, `Steps`, `ActiveCalories`, `HeartRateAvg`, `SleepData?`, `RecordedAt` | Following the cron job upon synchronizing wearable data. |
| `DeviceIntegration.FatSecretDataReceived` | `UserId`, `Meals`, `TotalCalories`, `Macros`, `RecordedAt` | Following the cron job upon synchronizing the external nutritional diary. |
| `DeviceIntegration.SyncFailed` | `UserId`, `DeviceSource`, `Reason`, `OccurredAt` | If an external API call fails (expired token, rate limit, etc.). |

Expected consumers: `BodyMetrics` (ScaleData), `Nutrition` (FatSecretData), `Dashboard` (all, for graphs), and potentially a future activity/`NEAT` module if the decision is made to cross-reference wearable steps with NEAT.

## Events Subscribed To

According to the diagram, **it does not consume events from other modules**. Its trigger is the `Cron jobs` (it is not a domain module, but rather infrastructure/scheduler) and potentially direct user actions (e.g., "sync now" or connecting/disconnecting an external account).

## Open Notes

- Define the authentication model for each external service (OAuth tokens per user): will it be stored inside this module or delegated to `auth`?
- Decide on cron granularity: one per integration (AmazFit, Scale, FatSecret) or a single orchestrator that distributes tasks?
- Retry and backoff policy if an external API does not respond.
- Decide whether unit normalization (kg vs lb, etc.) occurs here (before emitting the event) or is delegated to the consumer module.