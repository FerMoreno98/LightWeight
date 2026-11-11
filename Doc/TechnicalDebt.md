# Deuda técnica

Decisiones y problemas conocidos que hemos dejado para más adelante. Cada entrada explica qué pasa, por qué se dejó y cuándo conviene retomarla. Cuando una se resuelva, se borra de aquí.

## Arquitectura

### `LightWeight.Shared` no es agnóstico

- **Qué pasa:** depende de FluentMigrator, FluentValidation y `Microsoft.Extensions.DependencyInjection.Abstractions`, y `Microsoft.Extensions.Logging.Abstractions`. Como los proyectos Domain lo referencian, esas librerías también les llegan.
- **Por qué se dejó:** es un cambio amplio para lo que aporta ahora.
- **Cuándo retomarla:** si la aplicación escala mucho o algún módulo se extrae.
- **Solución prevista:** crear `LightWeight.Shared.Infrastructure` y mover ahí `Mediator`, `EventPublisher`, `EventDispatcher`, `ValidationPipelineBehavior` y `ModuleMigrationManager`, que son los únicos archivos que usan librerías externas. Conservando sus namespaces no cambia ningún `using`. Debe ir en una carpeta hermana de `src/shared/`, porque el csproj de Shared compila todo lo que hay bajo su carpeta.

### Servicios compartidos registrados en cada módulo

- **Qué pasa:** cada módulo registra por su cuenta `IMediator`, `IEventPublisher`, `IClock` y otros servicios. Training y user-profile añaden además el comportamiento de validación sin comprobar si ya existe, así que puede ejecutarse más de una vez por petición.
- **Por qué se dejó:** no rompe nada, y solo duplica trabajo.
- **Cuándo retomarla:** junto a la separación de Shared, o al añadir el próximo módulo.

## Autenticación

### La detección de robo del refresh token no hace nada

- **Qué pasa:** `DeviceToken.ValidateRefreshToken` lanza `StolenRefreshTokenException` cuando llega un token que ya se revocó y se reemplazó. Esto indica que alguien está reutilizando un token viejo. Nadie captura la excepción para revocar las sesiones del usuario: llega al `GlobalExceptionHandler` como `BusinessException` y se responde un 400.
- **Cuándo retomarla:** a la vez que la revocación por dispositivo y la carrera entre pestañas, porque están relacionadas.
- **Solución prevista:** al detectarla, revocar todos los refresh tokens activos del usuario, o al menos los del dispositivo. Antes hay que resolver la carrera entre pestañas; si no, un uso normal cerraría todas las sesiones.

### `DeviceToken.RevokedAt` e `IsActive` no se usan

- **Qué pasa:** ningún código modifica `RevokedAt` ni comprueba `IsActive` al refrescar. No se puede cerrar la sesión de un dispositivo concreto ni la de todos.
- **Cuándo retomarla:** cuando se quiera una pantalla de "dispositivos conectados" o un "cerrar todas las sesiones".
- **Solución prevista:** un método `User.RevokeDevice` / `RevokeAllDevices` que revoque el dispositivo y sus refresh tokens, y que `ValidateRefreshToken` rechace los tokens de dispositivos inactivos.

### Carrera de refresh entre pestañas

- **Qué pasa:** el access token vive en memoria y la cookie se comparte entre pestañas. Si dos pestañas recargan a la vez, las dos llaman a `/api/auth/refresh` con el mismo token. La primera lo rota y la segunda envía un token ya reemplazado, que salta como robo, y esa pestaña pierde la sesión.
- **Cuándo retomarla:** antes de implementar la reacción al robo.
- **Solución prevista:** en el front, una sola pestaña hace el refresh (Web Locks o `BroadcastChannel`) y comparte el token con las demás. En el backend, aceptar el token anterior durante unos segundos después de rotarlo.

### Los errores de refresh devuelven 400 o 404

- **Qué pasa:** un refresh token inválido, revocado o caducado responde 400, y un usuario o dispositivo inexistente responde 404. Lo correcto sería 401. El front trata cualquier error como sesión perdida, así que hoy no rompe nada.
- **Cuándo retomarla:** cuando haya otro cliente (app nativa) o al tocar el manejo de errores de auth.

### El dispositivo no se actualiza y puede quedar huérfano

- **Qué pasa:** si el dispositivo ya existe, `User.RegisterDevice` solo actualiza `LastSeenAt`; `DeviceName` y `Platform` se quedan con los valores del primer login. Si el usuario borra el `localStorage`, se genera otro `device_id` y el `DeviceToken` antiguo se queda con su refresh token hasta que caduque.
- **Cuándo retomarla:** con la pantalla de dispositivos conectados.

### El access token sigue siendo válido tras el logout

- **Qué pasa:** el logout solo revoca el refresh token. El JWT sigue siendo válido hasta que caduca (15 minutos en Development).
- **Por qué se dejó:** es lo habitual con access tokens cortos; revocarlos exigiría una lista negra.
- **Cuándo retomarla:** solo si se necesita revocación inmediata.

## Dashboard

### El rebuild reclasifica el histórico

- **Qué pasa:** `TemplateSet.UpdateSet` edita el plan sin comprobar si ya se entrenó, y el rebuild relee el plan actual. Tras un rebuild cambian la zona de repeticiones y el volumen de sesiones pasadas. Las repeticiones, el peso, el RPE y el 1RM de cada serie no cambian.
- **Por qué se dejó:** el botón Recalcular está comentado, así que solo pasa si se llama al endpoint a mano.
- **Cuándo retomarla:** antes de volver a exponer el rebuild al usuario.
- **Solución prevista:** copiar el rango y los músculos planificados en cada `Set` al registrarlo. Mientras no haya datos reales, no hace falta rellenar series antiguas.

### Botón Recalcular comentado

- **Qué pasa:** está oculto en el front, pero `POST /api/dashboard/rebuild` sigue disponible.
- **Por qué se dejó:** era una medida de seguridad para eventos perdidos, poco probables en un monolito modular.
- **Cuándo retomarla:** cuando una proyección nueva necesite cargar datos existentes, habrá que usar el endpoint. Conviene conservarlo.

### `EventPublisher` aísla a todos los suscriptores

- **Qué pasa:** si un suscriptor falla, se registra y la petición sigue. Es correcto para los proyectores, pero también se aplica a cualquier suscriptor futuro, y un rebuild con un fallo parcial responde 204.
- **Por qué se dejó:** hoy los suscriptores son los proyectores, los handlers de rebuild y un handler vacío de `UserCreated`.
- **Cuándo retomarla:** antes de añadir un suscriptor cuyo fallo deba cancelar la operación o avisar al usuario.

## Validación

### Excepciones genéricas

- **Qué pasa:** cualquier excepción que no sea `BusinessException` se responde como 500, incluidos los `ArgumentException` de los conversores de training. Es correcto si son fallos de programación, pero cualquiera que dependa de la entrada del usuario debería tener validador.
- **Cuándo retomarla:** al revisar los logs de errores, cuando exista Serilog.

## Infraestructura

### `LightWeight.slnx` vacío

- **Qué pasa:** el archivo no incluye ningún proyecto, así que el `dotnet restore` del Dockerfile no restaura nada real y los tests se lanzan proyecto a proyecto.
- **Cuándo retomarla:** cuando moleste al compilar o al montar CI.

## Tests

### Cobertura de integración

- **Qué pasa:** no hay tests de integración. `TrainingProjectionStore` solo se prueba con fakes, el front tiene un único spec y `App` no tiene proyecto de tests, así que `GlobalExceptionHandler` se ha verificado a mano.
- **Cuándo retomarla:** antes de desplegar, o cuando un fallo se escape a mano.
