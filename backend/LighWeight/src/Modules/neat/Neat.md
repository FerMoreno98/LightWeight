# Module: NEAT

## Responsibility

It tracks the physical activity the user performs outside of their structured training (outside of the `Training` module): playing tennis, football, long walks, etc. NEAT = Non-Exercise Activity Thermogenesis, meaning energy expenditure from activity not planned as resistance training but relevant to the user's total caloric balance.

It is a straightforward module focused on the logging (`Register`) of these activities.

## Main Entities / Aggregates

- **NEAT** (root aggregate) — logged extra physical activity.
  - `Register` — individual log/entry of an activity: type of activity, duration, perceived intensity or estimated calories, date.

## Events Emitted

| Event | Tentative Payload | When It Fires |
|---|---|---|
| `Neat.ActivityRegistered` | `UserId`, `RegisterId`, `ActivityType`, `DurationMinutes`, `EstimatedCalories`, `RecordedAt` | Upon logging a new extra physical activity. |
| `Neat.ActivityDeleted` | `UserId`, `RegisterId` | Upon deleting an activity log (if permitted). |

Mainly consumed by `Dashboard` to compute total daily caloric expenditure, and potentially by `Nutrition` if caloric goals need to be adjusted based on NEAT in the future.

## Events Subscribed To

For now, according to the diagram, **NEAT does not consume events from other modules** — it is a purely emitting module, driven by direct user input.

## Open Notes

- Define whether `ActivityType` is a closed catalog (enum: tennis, football, running, walking, cycling...) or an open text field with suggestions.
- Decide how `EstimatedCalories` is calculated: using an internal formula based on MET (Metabolic Equivalent of Task) + user weight (which would require reading data from `UserProfile`/`BodyMetrics`), or a manual estimation entered by the user?
- If MET is used, this module would become dependent on data from another module (current weight) — decide whether to resolve this via a direct cross-module query or by waiting for the data to arrive via an event and keeping it cached locally within NEAT.