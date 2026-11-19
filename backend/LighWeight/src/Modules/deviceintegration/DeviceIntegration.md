# Module: DeviceIntegration

## Responsibility

This is the integration module for external applications and devices: smart scales, smartwatches/wearables (AmazFit), and nutritional tracking apps (FatSecret). It calls the external APIs, normalizes the received data and publishes it as integration events for the rest of the modules to consume. It holds no business logic about the data itself: what a day of nutrition means is Nutrition's job.

**Done:** FatSecret (see below). **Not started:** scale and AmazFit.

## Events Emitted

| Event | Payload | When It Fires |
|---|---|---|
| `deviceintegration.fatsecret.diary.synced.v1` | `UserId`, `From`, `To`, `Days[{ Date, Calories, Protein, Carbohydrate, Fat }]` | After reading a period of the FatSecret diary (see *FatSecret*). Consumer: Nutrition |
| `DeviceIntegration.ScaleDataReceived` (sketch) | `UserId`, `WeightKg`, `BodyFatPercentage?`, `RecordedAt`, `DeviceSource` | Upon detecting a new scale reading |
| `DeviceIntegration.AmazFitDataReceived` (sketch) | `UserId`, `Steps`, `ActiveCalories`, `HeartRateAvg`, `SleepData?`, `RecordedAt` | Upon synchronizing wearable data |

Expected consumers of the sketches: `BodyMetrics` (scale), `Dashboard`, and potentially `NEAT` (wearable steps).

## Events Subscribed To

None. Its triggers are the nightly scheduler and the user's actions (linking or unlinking an account).

## FatSecret (decided and built 2026-11-19)

FatSecret is the user's nutrition app; LightWeight only reads the **daily totals** of the food diary (see `nutrition/Nutrition.md`).

### Linking (OAuth 1.0 three-legged, HMAC-SHA1)

The diary is only reachable with OAuth 1.0; OAuth 2.0 (client credentials) only serves public food searches.

1. **`POST /api/deviceintegration/fatsecret/link`** → the API asks FatSecret for a request token (`POST https://authentication.fatsecret.com/oauth/request_token` with `oauth_callback` = `FatSecret:CallbackUrl`), keeps it in the user's `FatSecretConnection` (encrypted, valid **15 minutes**) and returns `authorizeUrl` (`https://authentication.fatsecret.com/oauth/authorize?oauth_token=…`). The frontend sends the browser there.
2. The user authorizes LightWeight in FatSecret, which sends them back to the **frontend** route `/integrations/fatsecret/callback?oauth_token=…&oauth_verifier=…`.
3. That page calls **`POST /api/deviceintegration/fatsecret/link/complete`** `{ oauthToken, oauthVerifier }` **with the user's JWT**. The API checks that the token is the one this user started (before calling FatSecret), exchanges it (`GET https://authentication.fatsecret.com/oauth/access_token`), stores the access token and queues the **history import**.

Why the callback goes to the frontend and not to the API: the callback is a top-level navigation without the JWT. Completing it from the frontend ties the FatSecret account to the logged-in user, instead of trusting whoever arrives at an API URL with a token.

- **`GET /api/deviceintegration/fatsecret`** → `{ available, status: NotLinked|Linked|Revoked, linkedAt, historyImported }`. `available` is false without FatSecret keys on the server: the profile card is not shown.
- **`DELETE /api/deviceintegration/fatsecret`** → forgets the tokens. The days already imported stay in Nutrition.
- Linking again while linked keeps the current token working until the new one is completed. Completing a link imports the history again (it may be another FatSecret account).

### Data

`food_entries.get_month` v2: `GET https://platform.fatsecret.com/rest/food-entries/month/v2?date=<days since 1970>&format=json` → one `day` per day **with entries**: `date_int`, `calories`, `carbohydrate`, `protein`, `fat`. Quirks handled by `FatSecretMonthParser`: numbers come as strings, `day` is an object (not an array) when there is only one, and it is missing when the month is empty (`{"month": {"from_date_int": …, "to_date_int": …}}`). An answer without `month` throws. A missing nutrient counts as 0. Error **9** (invalid access token) → `FatSecretAccessRevokedException`.

### When it is read

No webhooks nor modified dates: changes are detected by reading again. Kept as simple as possible:

- **Nightly run at 03:30 Europe/Madrid** (`NightlySchedule`; that time always exists and is never repeated in Madrid). Single instance on the VPS; no Hangfire/Quartz. No sync when opening the app, no "sync now" button.
- Each run reads, for every linked user, **from the 1st of the previous month to today** (Europe/Madrid): always 2 calls, **one event**. That window catches the corrections of past days and a missed night, so there is no catch-up logic nor `LastSyncedAt`.
- **History import** (just linked, `HistoryImportedAt` null): month by month backwards from the current one, **one event per month** (`From` = 1st, `To` = last day or today), until **3 empty months in a row** or 120 months. If it fails halfway it stays pending and the next night starts it again. Three is enough on purpose (decided 2026-11-19): the diary is only worth what can be related to the sessions logged in LightWeight, so a user who stopped logging in FatSecret months ago does not need that old history.
- **Days after today are left out**: FatSecret lets the user plan meals ahead, and Nutrition rejects future days.
- The event carries `From`/`To` so Nutrition knows an absent day was emptied. Unchanged days produce no writes nor events downstream.
- **One queue, one consumer** (`FatSecretSyncQueue` + `FatSecretSyncWorker`): the scheduler and the link completion only enqueue user ids; the worker reads them one at a time, each in its own DI scope. So the nightly run and an import never read the same user at once. The queue is in memory: a restart loses it, and the next night queues everyone again.
- A failing user is logged and does not stop the others; there are no retries (the next night reads the same window). Error 9 marks the connection **revoked**: out of the nightly run, and the profile card asks to link again.

### Not used

`food_entries.get` (per food, one call per day), `weights.get_month` (BodyMetrics covers weight), `exercise_entries` (Training/NEAT cover it), `profile.get` (only weight/height/goal weight; no calorie or macro goals in the API).

## Configuration

Section `FatSecret` (env `FatSecret__…`, values in `.env`, see `.env.example`):

| Key | Notes |
|---|---|
| `ConsumerKey`, `ConsumerSecret` | From the application registered at platform.fatsecret.com (OAuth 1.0 "Consumer Key" and "Shared Secret") |
| `CallbackUrl` | Frontend page FatSecret sends the user back to. Set in the compose files: `http://localhost:4200/…` in dev, `https://${DOMAIN}/…` in prod |
| `TokenEncryptionKey` | Base64 of 32 bytes (`openssl rand -base64 32`). **Losing or changing it means every user has to link again** |

All four are needed; without them the app still starts, `available` is false and the scheduler does nothing.

## Implementation notes

- **Aggregate `FatSecretConnection`** (one per user, unique `UserId`): `Create`, `StartLinking`, `EnsurePendingRequest`, `CompleteLinking`, `MarkHistoryImported`, `MarkAccessRevoked`. `IsLinked` = access token and not revoked. No domain events: nothing else needs to know.
- **Application**: commands `StartFatSecretLinking` (returns the authorize URL), `CompleteFatSecretLinking`, `UnlinkFatSecret`; query `GetFatSecretConnection`; `FatSecretDiarySynchronizer` (history import or recent window, revoked handling). FatSecret is behind `IFatSecretClient` and the queue behind `IFatSecretSyncQueue`.
- **Infrastructure**: `FatSecretClient` (typed `HttpClient`, 30 s timeout, **default HttpClient loggers removed**: they would write `oauth_token` and the signature from the query string), `OAuth1Signer` (RFC 5849; tested against the classic Twitter example), `TokenProtector` (AES-256-GCM, `base64(nonce|tag|ciphertext)`, applied as an EF value converter to the four token columns), hosted services `FatSecretSyncWorker` and `FatSecretNightlyScheduler`.
- **Today** comes from `shared/Types/UserCalendar` (Europe/Madrid), the same one Nutrition uses.
- **Schema** `deviceintegration`, table `deviceintegration_FatSecretConnections`: `Id`, `UserId` (unique `Ux_FatSecretConnections_UserId`), `AccessToken`/`AccessTokenSecret`/`RequestToken`/`RequestTokenSecret` (text, encrypted), `LinkedAt`, `RevokedAt`, `HistoryImportedAt`, `RequestedAt` (timestamptz). Migrations `2026111920xx` (shared `VersionInfo`: keep using this range).
- **Frontend**: `Modules/DeviceIntegration` — `fatsecret-card` in the profile page (connect / reconnect / disconnect, "importing your history") and `fatsecret-callback-page` on `/integrations/fatsecret/callback`.
- **Tests**: `testing/LightWeight.DeviceIntegration.UnitTests` (domain, handlers, synchronizer, schedule across clock changes, signature, parser, encryption). Checked against the local Postgres (encrypted at rest, nightly query) and against the real FatSecret with a fake key (rejected → "not available right now").

## State

**Done (2026-11-19):** FatSecret linking, history import, nightly run, profile card. **Not tested with a real FatSecret account yet**: it needs the consumer key and secret of the registered application.

## Open Notes

- Authentication model of the other services (AmazFit, scale).
- Cron granularity, retry and backoff for the other integrations (FatSecret: one nightly run, no retry).
- Decide whether unit normalization (kg vs lb, etc.) occurs here (before emitting the event) or is delegated to the consumer module.
