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
| `deviceintegration.fatsecret.diary.synced.v1` | `UserId`, `From`, `To`, `Days[{ Date, Calories, Protein, Carbohydrate, Fat }]` | After each FatSecret synchronization (see *FatSecret*). Replaces the old `FatSecretDataReceived` sketch. |
| `DeviceIntegration.SyncFailed` | `UserId`, `DeviceSource`, `Reason`, `OccurredAt` | If an external API call fails (expired token, rate limit, etc.). |

Expected consumers: `BodyMetrics` (ScaleData), `Nutrition` (FatSecretData), `Dashboard` (all, for graphs), and potentially a future activity/`NEAT` module if the decision is made to cross-reference wearable steps with NEAT.

## Events Subscribed To

According to the diagram, **it does not consume events from other modules**. Its trigger is the `Cron jobs` (it is not a domain module, but rather infrastructure/scheduler) and potentially direct user actions (e.g., "sync now" or connecting/disconnecting an external account).

## FatSecret (decided 2026-11-19)

FatSecret is the user's nutrition app; LightWeight only reads the **daily totals** of the food diary (see `nutrition/Nutrition.md`).

- **Auth:** the diary is only reachable with **OAuth 1.0 three-legged** (HMAC-SHA1). OAuth 2.0 (client credentials) only serves public food searches, not user data. Flow: `POST https://authentication.fatsecret.com/oauth/request_token` (with `oauth_callback`) → redirect the user to `https://authentication.fatsecret.com/oauth/authorize?oauth_token=…` → callback with `oauth_verifier` → `GET https://authentication.fatsecret.com/oauth/access_token`. Store `oauth_token` + `oauth_token_secret` per user in this module, encrypted. The docs do not state a lifetime: treat error **9 (invalid access token)** as "disconnected" and ask the user to link again.
- **Data:** `food_entries.get_month` v2 (`GET https://platform.fatsecret.com/rest/food-entries/month/v2?date=<days since 1970>&format=json`) → one `day` per day **with entries**: `date_int`, `calories`, `carbohydrate`, `protein`, `fat`. JSON values come as strings. Days without entries are omitted.
- **No webhooks nor modified dates:** changes are detected by reading again. Decided on 2026-11-19, kept as simple as possible:
  - **Only a nightly run** (~03:30 Europe/Madrid), a `BackgroundService` in the host (single instance on the VPS; no Hangfire/Quartz). No sync when opening the app, no "sync now" button.
  - Each run reads, for every linked user, **from the 1st of the previous month to today** (Europe/Madrid): always 2 calls to `food_entries.get_month`. That window catches the corrections of past days (between 30 and 60 days back) and, if the server was down one night, the next run catches up — so there is no catch-up logic nor `LastSyncedAt` to keep.
  - **On linking the account** the history is backfilled once, month by month backwards.
  - The event carries `From`/`To` so Nutrition knows that an absent day was emptied. Unchanged days produce no writes nor events (`DiaryDay.Update`).
  - A failing user is logged and does not stop the others; a transient error is simply retried the next night; error 9 marks the connection as unlinked.
- **Not used:** `food_entries.get` (per food, one call per day), `weights.get_month` (BodyMetrics covers weight), `exercise_entries` (Training/NEAT cover it), `profile.get` (only weight/height/goal weight; no calorie or macro goals in the API).

## Open Notes

- Authentication model of the other services (AmazFit, scale). For FatSecret the tokens live in this module (see above).
- Decide on cron granularity for the other integrations (FatSecret: one nightly run, see above).
- Retry and backoff policy for the other integrations (FatSecret: no retry, the next night reads the same window again).
- Decide whether unit normalization (kg vs lb, etc.) occurs here (before emitting the event) or is delegated to the consumer module.