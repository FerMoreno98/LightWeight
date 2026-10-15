# Module: BodyMetrics

## Responsibility

Registers the body measurements of the user so their evolution can be followed: **body weight**, **perimeters** and **skinfolds**. The user can register a measurement, correct it and delete it, and see the latest ones to check what was just written.

This module owns the measurements, but it does not analyse them. Long-term evolution, trends and charts that cross data with other modules (e.g. body weight against training volume) belong to the **Dashboard**, which receives every measurement through the integration events published here. That is why the history endpoints only return the **latest 10** measurements of each kind.

## Domain Model

### Aggregates (all extend `AggregateRoot<Guid>`)

| Aggregate | Properties | Unit | DB Table |
|-----------|-----------|------|----------|
| `WeightMeasurement` | `UserId`, `Weight`, `MeasuredAt` | kg | `bodymetrics_WeightMeasurements` |
| `Perimeters` | `UserId`, `Measures` (`PerimeterMeasures`), `MeasuredAt` | cm | `bodymetrics_Perimeters` |
| `SkinFolds` | `UserId`, `Measures` (`SkinFoldMeasures`), `MeasuredAt` | mm | `bodymetrics_SkinFolds` |

Each aggregate is one measurement taken at a moment. They have no child entities and are independent from each other: a day can have a weight and no perimeters, and several measurements of the same kind can share a day.

### Value Objects

| Value Object | Properties | Rules |
|-------------|-----------|-------|
| `PerimeterMeasures` | `Neck`, `RightArmRelaxed`, `RightArmTensioned`, `LeftArmRelaxed`, `LeftArmTensioned`, `Chest`, `Shoulder`, `Waist`, `Hip`, `Abs`, `RightThigh`, `LeftThigh`, `RightCalf`, `LeftCalf` (all `decimal?`) | At least one; each one `0 < x ≤ 300` cm |
| `SkinFoldMeasures` | `Abdominal`, `Suprailiac`, `Tricipital`, `Subscapular`, `Thigh`, `Calf` (all `decimal?`) + calculated `Summatory` | At least one; each one `0 < x ≤ 100` mm. `Summatory` only when the six are present |

The body weight is a single `decimal` validated by the aggregate: `20 ≤ x ≤ 400` kg.

### Enums

| Enum | Values | Used by |
|------|--------|---------|
| `Sex` | `Male`, `Female` | `SkinFolds.CalculateEstimatedFatPercentage(Sex)` |

### Behaviour

| Operation | Methods | Event raised |
|-----------|---------|--------------|
| Register | `WeightMeasurement.Create`, `Perimeters.Create`, `SkinFolds.Create` | `…RegisteredDomainEvent` |
| Correct | `Correct(...)` — replaces the values and the date (the whole set of perimeters/skinfolds) | `…CorrectedDomainEvent` |
| Delete | `Delete()` — raises the event; the repository removes the row | `…DeletedDomainEvent` |
| Fat percentage | `SkinFolds.CalculateEstimatedFatPercentage(Sex)` — linear formula on the summatory (null without the six skinfolds) | — |

Common rules: the user id cannot be empty, and a measurement cannot be taken in the future (`MeasuredAt ≤ now + 5 min`, the margin absorbs the client's clock).

## Events Emitted

Domain events are translated into integration events by the handlers in `Application/Events`. **Registering and correcting publish the same `…Recorded` event** with the full values: consumers upsert by `MeasurementId`. Integration events only use primitive types, so consumers do not depend on this module's domain.

| Integration event | Name | Payload | Raised when |
|-------------------|------|---------|-------------|
| `WeightRecordedIntegrationEvent` | `bodymetrics.weight.recorded.v1` | `MeasurementId`, `UserId`, `Weight`, `MeasuredAt`, `OccurredAtUtc` | A weight is registered or corrected |
| `WeightDeletedIntegrationEvent` | `bodymetrics.weight.deleted.v1` | `MeasurementId`, `UserId`, `MeasuredAt`, `OccurredAtUtc` | A weight is deleted |
| `PerimetersRecordedIntegrationEvent` | `bodymetrics.perimeters.recorded.v1` | `MeasurementId`, `UserId`, `MeasuredAt`, the 14 perimeters (null if not measured), `OccurredAtUtc` | Perimeters are registered or corrected |
| `PerimetersDeletedIntegrationEvent` | `bodymetrics.perimeters.deleted.v1` | `MeasurementId`, `UserId`, `MeasuredAt`, `OccurredAtUtc` | Perimeters are deleted |
| `SkinFoldsRecordedIntegrationEvent` | `bodymetrics.skinfolds.recorded.v1` | `MeasurementId`, `UserId`, `MeasuredAt`, the 6 skinfolds, `Summatory`, `OccurredAtUtc` | Skinfolds are registered or corrected |
| `SkinFoldsDeletedIntegrationEvent` | `bodymetrics.skinfolds.deleted.v1` | `MeasurementId`, `UserId`, `MeasuredAt`, `OccurredAtUtc` | Skinfolds are deleted |

`MeasuredAt` is when the user took the measurement; `OccurredAtUtc`, when it was recorded. The fat percentage is not published because it needs the user's sex, which belongs to UserProfile: consumers calculate it.

Contracts: `shared/Contracts/BodyMetrics/BodyMetricsIntegrationEvents.cs`, names in `shared/Contracts/EventNames.cs` (`EventNames.BodyMetrics`).

## Events Subscribed To

| Integration event | Name | Origin | Action |
|---|---|---|---|
| `RebuildRequestedIntegrationEvent` | `dashboard.rebuild.requested.v1` | Dashboard | Publishes again a `…Recorded` event for **every** measurement of the user (weights, perimeters and skinfolds), built from the current rows. |

Events are not stored anywhere, so they cannot be replayed: when the Dashboard rebuilds its projections it empties them and asks the sources to regenerate the events from their current state (`Application/Events/RebuildRequestedIntegrationEventHandler.cs`). The regenerated events are the same `…Recorded` events as always, with `OccurredAtUtc` set to the time of the rebuild; consumers cannot tell them from new ones and upsert them.

## API

All endpoints require authentication and work on the measurements of the authenticated user (`/api/bodymetrics`).

| Method | Route | Body | Response |
|--------|-------|------|----------|
| `GET` | `/weights`, `/perimeters`, `/skinfolds` | — | Latest 10 measurements, from the most recent |
| `POST` | `/weights`, `/perimeters`, `/skinfolds` | `{ measuredAt, ...values }` | `{ id }` |
| `PUT` | `/{kind}/{measurementId}` | `{ measuredAt, ...values }` | 204 — replaces the whole measurement (values not sent become null) |
| `DELETE` | `/{kind}/{measurementId}` | — | 204 |

Bodies are flat: `{ "measuredAt": "2026-10-14T07:30:00Z", "weight": 82.4 }`, `{ "measuredAt": "...", "neck": 38.5, "waist": 82.1 }`. A date without time zone is taken as UTC.

## Open Notes

* **Error responses:** domain and application exceptions (value out of range, measurement of another user, not found) currently reach the client as 500, like in the rest of the project, until a global exception handler maps them to 400/403/404.
* **Older measurements:** only the latest 10 are shown and can be corrected from the app. The rest stay stored and are part of the events; the Dashboard will show the whole evolution.
* **Fat percentage:** available in the domain (`CalculateEstimatedFatPercentage`) but not exposed by the API yet, because it needs the sex of the user (UserProfile).
