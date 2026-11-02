# Deuda técnica

Decisiones y problemas conocidos que hemos dejado para más adelante. Cada entrada explica qué pasa, por qué se dejó y cuándo conviene retomarla. Cuando una se resuelva, se borra de aquí.

## Arquitectura

### `LightWeight.Shared` no es agnóstico

- **Qué pasa:** depende de FluentMigrator, FluentValidation y `Microsoft.Extensions.DependencyInjection.Abstractions`. Como los proyectos Domain lo referencian, esas librerías también les llegan.
- **Por qué se dejó:** es un cambio amplio para lo que aporta ahora.
- **Cuándo retomarla:** si la aplicación escala mucho o algún módulo se extrae.
- **Solución prevista:** crear `LightWeight.Shared.Infrastructure` y mover ahí `Mediator`, `EventPublisher`, `EventDispatcher`, `ValidationPipelineBehavior` y `ModuleMigrationManager`, que son los únicos archivos que usan librerías externas. Conservando sus namespaces no cambia ningún `using`. Debe ir en una carpeta hermana de `src/shared/`, porque el csproj de Shared compila todo lo que hay bajo su carpeta.

### Servicios compartidos registrados en cada módulo

- **Qué pasa:** cada módulo registra por su cuenta `IMediator`, `IEventPublisher`, `IClock` y otros servicios. Training y user-profile añaden además el comportamiento de validación sin comprobar si ya existe, así que puede ejecutarse más de una vez por petición.
- **Por qué se dejó:** no rompe nada, y solo duplica trabajo.
- **Cuándo retomarla:** junto a la separación de Shared, o al añadir el próximo módulo.

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

## Validación

### Email inválido en el login devuelve 500

- **Qué pasa:** `POST /api/auth/otp/send` no valida el formato del email. Con un valor como `nope`, salta un `FormatException` al enviar el correo y se responde 500 en lugar de 400.
- **Por qué se dejó:** apareció al probar el manejador global de excepciones, y no forma parte de ese cambio.
- **Cuándo retomarla:** antes de desplegar.
- **Solución prevista:** añadir un validador a `SendOtpCodeCommand`.

### Excepciones genéricas

- **Qué pasa:** cualquier excepción que no sea `BusinessException` se responde como 500, incluidos los `ArgumentException` de los conversores de training. Es correcto si son fallos de programación, pero cualquiera que dependa de la entrada del usuario debería tener validador.
- **Cuándo retomarla:** al revisar los logs de errores, cuando exista Serilog.

## Infraestructura

### `LightWeight.slnx` vacío

- **Qué pasa:** el archivo no incluye ningún proyecto, así que el `dotnet restore` del Dockerfile no restaura nada real y los tests se lanzan proyecto a proyecto.
- **Cuándo retomarla:** cuando moleste al compilar o al montar CI.

### Vulnerabilidad en `System.Security.Cryptography.Pkcs` 9.0.0

- **Qué pasa:** NuGet avisa de una vulnerabilidad de gravedad alta y otra moderada al compilar `LightWeight.Shared`.
- **Cuándo retomarla:** antes de desplegar.

### Caché de `index.html`

- **Qué pasa:** `nginx.conf` no define `Cache-Control`, y tras un build el navegador puede seguir usando la versión anterior.
- **Cuándo retomarla:** antes de desplegar.
- **Solución prevista:** añadir `Cache-Control: no-cache` para `/index.html`.

## Tests

### Cobertura de integración

- **Qué pasa:** no hay tests de integración. `TrainingProjectionStore` solo se prueba con fakes, el front tiene un único spec y `App` no tiene proyecto de tests, así que `GlobalExceptionHandler` se ha verificado a mano.
- **Cuándo retomarla:** antes de desplegar, o cuando un fallo se escape a mano.
