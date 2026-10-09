# LightWeight

Aplicación para planificar y registrar entrenamientos de fuerza / culturismo, con la vista puesta en convertirse en una plataforma completa de seguimiento físico: entrenamiento, nutrición, métricas corporales, actividad diaria (NEAT) e integración con dispositivos.

- **Backend:** .NET 10 · ASP.NET Core Minimal APIs · EF Core + PostgreSQL · FluentMigrator · FluentValidation
- **Frontend:** Angular 22 (standalone components + signals) · Tailwind CSS 4 · Bun
- **Arquitectura:** monolito modular con DDD y CQRS ligero (mediator propio)

---

## Índice

- [Estado del proyecto](#estado-del-proyecto)
- [Arquitectura](#arquitectura)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Puesta en marcha](#puesta-en-marcha)
- [Módulos](#módulos)
- [Tests](#tests)
- [Convenciones](#convenciones)

---

## Estado del proyecto

| Módulo | Backend | Frontend | Descripción |
|---|:-:|:-:|---|
| **Auth** | ✅ | ✅ | Login sin contraseña por OTP al email, JWT + refresh token rotativo |
| **UserProfile** | ✅ | — | Datos personales y fase de entrenamiento (volumen / definición / mantenimiento) |
| **Training** | 🚧 | 🚧 | Programas, plantillas, sesiones y series; ciclos de planificación |
| **BodyMetrics** | 🧩 | — | Peso, % graso, medidas (solo dominio) |
| **Nutrition** | 🧩 | — | Diario nutricional y macros |
| **NEAT** | 🧩 | — | Actividad física fuera del entrenamiento |
| **DeviceIntegration** | 📝 | — | Básculas, wearables (AmazFit), apps de nutrición (FatSecret) |
| **Dashboard** | 📝 | — | Read model para gráficas a partir de eventos del resto de módulos |

✅ funcional · 🚧 en desarrollo · 🧩 esqueleto de proyectos · 📝 solo diseño (`.md`)

---

## Arquitectura

### Monolito modular

Una única aplicación (`LightWeight.App`) aloja todos los módulos. Cada módulo es independiente, tiene su propio esquema en PostgreSQL y se comunica con los demás **solo mediante eventos de integración** (contratos en `shared/Contracts`), nunca accediendo a las tablas o clases de otro módulo.

Cada módulo sigue la misma estructura en capas:

```
LightWeight.<Modulo>.Domain          Agregados, entidades, value objects, eventos de dominio, interfaces de repositorio
LightWeight.<Modulo>.Application     Commands / Queries + handlers, validadores, excepciones de aplicación
LightWeight.<Modulo>.Infrastructure  EF Core (DbContext, configuraciones), repositorios, migraciones FluentMigrator
LightWeight.<Modulo>.Api             Endpoints Minimal API y DTOs
testing/LightWeight.<Modulo>.UnitTests
```

Dependencias: `Api → Application → Domain`, `Infrastructure → Domain`. El dominio no conoce a nadie.

### Shared kernel (`backend/LighWeight/src/shared`)

| Carpeta | Contenido |
|---|---|
| `BuildingBlocks` | `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, utilidades de persistencia |
| `Mediator` | Mediator propio: `ICommand`, `IQuery`, sus handlers e `IMediator` (`SendAsync` / `QueryAsync`) |
| `Behavior` | Pipeline behaviors; `ValidationPipelineBehavior` ejecuta los validadores de FluentValidation antes de cada handler |
| `Messaging` | Despacho de eventos de dominio y publicación de eventos de integración entre módulos |
| `Contracts` | Eventos de integración públicos (p. ej. `UserCreatedIntegrationEvent`) |
| `Migrations` | `MigrationOrchestrator`: ejecuta las migraciones de cada módulo al arrancar |
| `Types` | `IClock` / `SystemClock` para poder controlar el tiempo en tests |

### Arranque de la API

`LightWeight.App/Program.cs` registra los módulos (`AddAuthModule`, `AddUserProfileModule`, `AddTrainingModule`), configura CORS y JWT y, al arrancar:

1. Ejecuta las migraciones de todos los módulos (`RunModuleMigrationsAsync`).
2. Carga los datos iniciales del módulo Training, como el catálogo de ejercicios (`SeedTrainingDataAsync`).
3. Mapea los endpoints de cada módulo.

### Frontend (`frontend/LightWeight/src/app`)

Organizado por módulos, igual que el backend:

```
Core/Auth            Interceptor HTTP (añade el Bearer y refresca el token ante un 401) y almacenamiento del token
Shared/navbar        Barra de navegación
Modules/<Modulo>/
  data/              Servicio de API (HttpClient) y tipos
  state/             Store basado en signals (isLoading, error, datos)
  UI/                Componentes standalone por pantalla
  <Modulo>.Routes.ts Rutas cargadas de forma lazy
```

---

## Estructura del repositorio

```
.
├── backend/LighWeight/
│   ├── src/
│   │   ├── App/LightWeight.App/   Host de la API (Program.cs, appsettings, extensiones de arranque)
│   │   ├── Modules/               Un directorio por módulo (auth, training, user-profile, ...)
│   │   └── shared/                Shared kernel
│   └── Dockerfile
├── frontend/LightWeight/          Aplicación Angular (+ Dockerfile y nginx.conf)
├── Doc/                           Diagramas (TrainingModuleDiagram.drawio)
├── docker-compose.yaml            API + PostgreSQL + Mailpit + frontend (solo desarrollo)
└── .env.example                   Variables que necesita docker compose (copiar a .env)
```

---

## Puesta en marcha

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Bun](https://bun.sh) 1.3+ (gestor de paquetes del frontend)
- [Docker](https://www.docker.com/) (para PostgreSQL y Mailpit, o para levantarlo todo)

La API siempre queda en `http://localhost:5196/api`, tanto con Docker como con `dotnet run`, que es la URL que usa el frontend (`frontend/LightWeight/src/Environments/Environment.ts`). No hace falta cambiar nada en Angular según cómo arranques la API.

### Opción A — Todo con Docker

```bash
cp .env.example .env      # y rellena JWT_SECRET (p. ej. con: openssl rand -base64 48)
docker compose up --build
```

| Servicio | URL |
|---|---|
| Frontend | http://localhost:4200 |
| API | http://localhost:5196/api |
| Mailpit (bandeja de los OTP) | http://localhost:8025 |
| PostgreSQL | `localhost:5432` · BD `lightweight` |

> `docker-compose.yaml` está pensado **solo para desarrollo local**: usa credenciales de PostgreSQL por defecto, `ASPNETCORE_ENVIRONMENT=Development` y expone la base de datos y Mailpit en tu máquina. No lo uses tal cual en un servidor.

### Opción B — Desarrollo local

**1. Infraestructura** (PostgreSQL y Mailpit)

```bash
docker compose up -d db mailpit
```

**2. Configuración del backend**

Crea `backend/LighWeight/src/App/LightWeight.App/appsettings.Development.json`. Está en `.gitignore` y en `.dockerignore`, así que no se sube a git ni se copia a la imagen Docker:

```json
{
  "Cors":   { "AllowedOrigins": ["http://localhost:4200"] },
  "Smtp":   { "Host": "localhost", "Port": 1025 },
  "Jwt": {
    "Secret": "<cadena aleatoria de al menos 32 caracteres; genera la tuya, no reutilices ejemplos>",
    "Issuer": "LightWeight.Api",
    "Audience": "LightWeight",
    "AccessTokenExpirationMinutes": 15
  },
  "ConnectionStrings": {
    "defaultConnection": "Host=localhost;Port=5432;Username=<usuario>;Password=<contraseña>;Database=lightweight"
  }
}
```

**3. API**

```bash
cd backend/LighWeight/src/App/LightWeight.App
dotnet run --launch-profile http        # http://localhost:5196
```

Las migraciones se aplican solas al arrancar.

**4. Frontend**

```bash
cd frontend/LightWeight
bun install
bun run start                           # http://localhost:4200
```

**5. Primer login**

El login es sin contraseña: escribe cualquier email en http://localhost:4200 y recoge el código en Mailpit (http://localhost:8025). El usuario se crea automáticamente la primera vez.

### Secretos y configuración

- Ningún secreto se versiona. Los valores sensibles van en `appsettings.Development.json` (con `dotnet run`) o en `.env` (con Docker). Los dos están en `.gitignore`, y `appsettings.Development.json` también está excluido de la imagen Docker.
- Las credenciales de PostgreSQL de `docker-compose.yaml` son solo para el contenedor local; úsalas en `<usuario>` y `<contraseña>` si trabajas contra él. Si usas otra base de datos (por ejemplo en la nube), su cadena de conexión debe ir solo en tu `appsettings.Development.json`.
- Si un secreto llega a subirse por error, no basta con borrarlo en un commit nuevo: hay que **rotarlo** (cambiar la contraseña o la clave), porque sigue en el historial.

---

## Módulos

Cada módulo documenta su modelo de dominio, eventos, endpoints y reglas de negocio en su propia carpeta:

| Módulo | Documentación |
|---|---|
| Auth | [`Modules/auth`](backend/LighWeight/src/Modules/auth) |
| UserProfile | [`Modules/user-profile`](backend/LighWeight/src/Modules/user-profile) |
| Training | [`Modules/training`](backend/LighWeight/src/Modules/training) · [diagrama](Doc/TrainingModuleDiagram.drawio) |
| BodyMetrics | [`Modules/bodymetrics`](backend/LighWeight/src/Modules/bodymetrics) |
| Nutrition | [`Modules/nutrition`](backend/LighWeight/src/Modules/nutrition) |
| NEAT | [`Modules/neat`](backend/LighWeight/src/Modules/neat) |
| DeviceIntegration | [`Modules/deviceintegration`](backend/LighWeight/src/Modules/deviceintegration) |
| Dashboard | [`Modules/dashboard`](backend/LighWeight/src/Modules/dashboard) |

En cada carpeta:

- `<Modulo>.md`: descripción funcional (responsabilidad, agregados, eventos, endpoints).
- `AGENTS.md`: invariantes y reglas no evidentes (también sirve de contexto para asistentes de IA).

Los endpoints de cada módulo cuelgan de `/api/<modulo>` y se definen en `LightWeight.<Modulo>.Api/<Modulo>Module.cs`. Salvo los de login, todos requieren `Authorization: Bearer <token>`, y el `UserId` se obtiene siempre del token. Puedes probarlos con `backend/LighWeight/src/App/LightWeight.App/LightWeight.App.http`.

---

## Tests

Tests unitarios con **xUnit**, **NSubstitute** y **FluentAssertions**, uno por módulo en `Modules/<modulo>/testing/`:

```bash
# Un módulo concreto
dotnet test backend/LighWeight/src/Modules/training/testing/LightWeight.Training.UnitTests

# Todos los módulos
for p in backend/LighWeight/src/Modules/*/testing/*/; do dotnet test "$p"; done
```

Frontend (Vitest):

```bash
cd frontend/LightWeight && bun run test
```

---

## Convenciones

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) con el módulo como scope, p. ej. `feat(training): ...`, `fix(auth): ...`, `refactor(training): ...`.
- **Base de datos:** un esquema por módulo (`auth`, `userprofile`, `training`, ...) y tablas con prefijo del módulo (`training_Programs`). Las migraciones son **FluentMigrator**, con versión `yyyyMMddHHmm`. No modifiques una migración ya aplicada: crea una nueva.
- **Enums:** se guardan como texto en la base de datos.
- **Casos de uso:** un comando o consulta por carpeta (`Commands/<Agregado>/<Accion>/`) con su `Command`, su `Handler` y su `Validator`, registrados en el `DependencyInjection.cs` del módulo.
- **Frontend:** componentes standalone, estado en stores con signals y navegación siempre con `routerLink` / `Router` (nunca `href`, que recarga la app).
