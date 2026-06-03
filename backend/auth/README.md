# LightWeight.Auth

Module responsible for user **authentication and authorisation** (registration, login, JWT, refresh tokens). Cross-module communication is performed in-process via the dispatcher (see `backend/shared/Messaging`).

---

## Emitted events
- `identity.user.registered.v1`
- `identity.user.disabled.v1`
- `identity.deviceToken.added.v1`


## Consumed events
- None


---

## Infrastructure

- REST endpoints exposed according to responsibilities.

