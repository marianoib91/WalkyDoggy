# Convención de nombres de WalkyDoggy

El código nuevo se escribe **en español**: variables, parámetros, métodos, clases internas y comentarios.
Los nombres no llevan tildes ni eñ (`tamano`, `Validacion`), para evitar problemas de codificación.

## Qué queda en inglés (y por qué)

Estos nombres forman parte de un contrato con algo externo; renombrarlos rompe cosas sin que el compilador avise:

| Qué | Ejemplos | Motivo |
|---|---|---|
| Entidades, tablas y columnas | `Walker`, `Walk`, `WalkerId`, `FinishedAt` | Mapean a la base de datos |
| DTOs y criterios | `WalkerDto`, `BookingDto`, propiedades | Viajan como JSON entre servidor y navegador |
| Controladores, acciones y sus parámetros | `WalksController.Finish`, `walkerId` | Forman las rutas de la API y Web API enlaza parámetros por nombre |
| Constantes con valores guardados | `WalkStatus.Pending`, `PaymentMethods.Cash` | El valor ("Pending", "Cash") se guarda en la base y viaja en el JSON |
| Campos de datos de la API en JavaScript | `reserva.walkerName`, `reserva.status` | Son los nombres que manda el servidor |
| Términos técnicos y de librerías | `token`, `url`, `callback`, `config`, `$scope` | Son vocabulario estándar |

## Vocabulario

| Inglés | Español | | Inglés | Español |
|---|---|---|---|---|
| walker | paseador | | payment | pago |
| customer | cliente | | gateway | pasarela |
| pet | mascota | | repository | repositorio |
| walk | paseo | | service | servicio |
| booking | reserva | | unit of work | unidad de trabajo |
| rating | valoración | | request / response | pedido / respuesta |
| price / rate | precio / tarifa | | criteria | criterio |
| work day | jornada | | error | error |
| city / province | ciudad / provincia | | user / role | usuario / rol |
| breed / size | raza / tamaño | | notification | notificación |

## Cómo se nombra cada cosa

- Variables y parámetros: `camelCase` en español (`repositorioPaseadores`, `idCliente`).
- Métodos y clases: `PascalCase` en español (`ObtenerReservasDelPaseador`, `ServicioPaseos`).
- Interfaces: `I` + nombre (`IServicioPaseos`).
- JavaScript: funciones de `$scope` y servicios en español (`pagar`, `servicioApi`); los controladores terminan en `Ctrl`.
- Cuando un campo es "de la API", se deja tal cual lo manda el servidor aunque el resto esté en español
  (por ejemplo `servicioApi.get('/api/walks/...')` o `reserva.paymentStatus`).
