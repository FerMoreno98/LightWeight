# Auth module

Manages user identity. Its sole responsibility is to verify who the user is and issue access tokens. It knows nothing about the user's profile or business logic.

The authentication flow is exclusively via OTP: the user enters their email, receives a one-time code, verifies it, and obtains an `AccessToken` + `RefreshToken`. No passwords.

## Aggregate root

`AuthUser` — represents the user's minimal identity in the system. Contains only the data required for authentication: contact identifier (email), verification status, and active tokens.

## Entities and value objects

- `OtpCode` — one-time code with expiration. Entity.
- `DeviceToken` — token associated with the device used to authenticate. Entity.
- `RefreshToken` — long-lived refresh token. Entity.
- `AuthUser` - Projection of user on the auth module
## Emitted events

| Event | When | Relevant payload |
|---|---|---|
| `UserRegistered` | First successful OTP verification | `UserId`, `ContactIdentifier`, `RegisteredAt` |

## Listened events

None. Auth has no dependencies on other modules.

## Notes

- The `UserId` generated here is the integration key across all modules in the system. No module generates its own user identifier.
- The `AccessToken` includes the `UserId` in its payload so that UserProfile can identify the user during onboarding without needing to call Auth directly.