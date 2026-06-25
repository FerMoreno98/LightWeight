# Module: Nutrition

## Responsibility

It manages the user's daily nutritional log: what they eat, when they eat, and the caloric/macronutrient summary for each day (`DiaryDay`). It can be populated either through manual user input or via data synchronized externally through `DeviceIntegration` (FatSecret).

## Main Entities / Aggregates

- **Nutrition** (root aggregate).
  - `DiaryDay` — the central entity: represents the nutritional diary of a specific day for a user (logged meals, calorie totals, and macros).

## Events Emitted

| Event | Tentative Payload | When It Fires |
|---|---|---|
| `Nutrition.DiaryDayCreated` | `UserId`, `DiaryDayId`, `Date` | Upon creating the first nutritional entry for a given day. |
| `Nutrition.DiaryDayUpdated` | `UserId`, `DiaryDayId`, `Date`, `TotalCalories`, `Macros` | Every time a meal is added, edited, or deleted, changing the daily totals. |
| `Nutrition.DiaryDayClosed` | `UserId`, `DiaryDayId`, `Date`, `TotalCalories`, `Macros` | If a concept of "closing" the day exists (e.g., past midnight or upon user confirmation), useful for giving Dashboard consolidated data. |

Consumed by `Dashboard` for calorie/macro trend graphs, and potentially by `Training` if a future requirement arises to correlate nutrition with performance.

## Events Subscribed To

| Origin Event | Origin Module | Action Upon Receipt |
|---|---|---|
| `DeviceIntegration.FatSecretDataReceived` | DeviceIntegration | Create or update the corresponding `DiaryDay` for that date using the synchronized data, avoiding the duplication of manually entered meals (conflict/merge policy to be defined). |

## Open Notes

- Define how a "meal" is modeled inside `DiaryDay`: a list of items with their own macros, or just aggregate totals per meal?
- Conflict policy when the same day contains both manual and FatSecret data: do they merge, does one take priority, or are both shown separately?
- Decide whether nutritional goals (target calories/macros) reside in this module or in `UserProfile`/`Training` (linked to the phase of the mesocycle: bulking, cutting, etc.).