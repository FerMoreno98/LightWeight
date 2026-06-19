# UserProfile module

Manages the user's static physical and personal data. It is populated during onboarding, right after the first authentication. Its data is the foundation for calculations in other modules such as basal metabolic rate, body fat percentage, or calorie targets.

It does not handle authentication, device data, or any metric that changes over time — those belong to other modules.

## Aggregate root

`UserProfileUser` — the user's profile. Contains static or rarely-changing attributes: name, surname, date of birth, biological sex, and height.

## Entities and value objects

- `Height` — height in centimetres. Value object.
- `Sex` — enum used by other modules for formula-based calculations. Value object.

## Emitted events

| Event | When | Relevant payload |
|---|---|---|
| `UserProfileCompleted` | User finishes the onboarding form | `UserId`, `HeightCm`, `DateOfBirth`, `Sex` |
| `UserProfileUpdated` | User modifies any profile field | `UserId`, changed fields |

## Listened events

| Event | From | Why |
|---|---|---|
| `UserRegistered` | Auth | Creates an empty profile record tied to the new `UserId` |

## Notes

- `UserProfileCompleted` is one of the most consumed events in the system. BodyMetrics, Nutrition, and Training all listen to it to enrich their local user projections with the physical data they need for their own calculations.
- Height is stored here as a static value. If the user updates it, `UserProfileUpdated` is emitted and consuming modules can react accordingly.
- This module does not store any metric that varies over time (weight, body fat, measurements). Those belong to BodyMetrics.