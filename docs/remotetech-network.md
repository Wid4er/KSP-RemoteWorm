# Red, rutas y retardo de RemoteTech 1.9.12

## Antenas y objetivos

`ModuleRTAntenna` implementa `IAntenna`. Una direccional se identifica por
`CanTarget` y `Dish > 0`; una omni tiene `Omni > 0`. El objetivo se guarda como
`RTAntennaTarget` (`Guid`) y se persiste manualmente.

`IAntenna.Guid` identifica la nave/satélite, no una antena individual: todas las
antenas de una misma nave devuelven el mismo valor. RTWB usa el `flightID` de la
pieza como identidad persistente de antena.

Los cuerpos celestes aparecen en la UI de objetivos con `CelestialBody.Guid()`,
un GUID determinista generado a partir del nombre. Por tanto, una antena apunta
al agujero local cuando:

```text
antenna.Target == wormholeBody.Guid()
```

`DishAngle` está expresado en grados y es el **ángulo total**. Al cargar:

```text
RTDishCosAngle = cos(DishAngle / 2 * pi / 180)
```

La comparación de cobertura de RTWB debe usar el semiancho
`acos(CosAngle)`, o `DishAngle / 2`, no `DishAngle` completo.

`Powered` refleja `IsRTPowered`; se actualiza en `FixedUpdate` según activación,
rotura, presión dinámica y consumo del recurso (por defecto `ElectricCharge`).

## Construcción del grafo

`NetworkManager.OnPhysicsUpdate()` reparte la actualización entre 50 ticks. Para
cada satélite seleccionado:

1. llama a `UpdateGraph(s)`;
2. inmediatamente llama a `FindPath(s, commandStations)` en Flight, Tracking
   Station y ciertas consultas de Space Center.

`UpdateGraph` calcula `GetLink(a,b)` contra todos los nodos, emite eventos de
eliminación, vacía `Graph[a.Guid]`, añade los enlaces normales y emite eventos de
alta. El grafo es dirigido; la bidireccionalidad normal emerge cuando se actualiza
cada origen.

`NetworkLink<ISatellite>` no almacena distancia. Su igualdad solo compara el
`Target`, de modo que RTWB no puede añadir dos tipos de enlace paralelos entre la
misma pareja.

## Alcance y línea de visión

`NetworkManager.GetLink` rechaza blackout y línea de visión bloqueada antes de
delegar en `RangeModelStandard` o `RangeModelRoot`. Ambos modelos seleccionan
antenas activas según `Omni`, `Dish`, objetivo directo, objetivo de nave activa o
campo de visión.

La distancia normal es siempre `Vector3d.Distance` entre las posiciones de los
satélites. El enlace de agujero no puede pasar por `GetLink`, porque eso volvería
a aplicar distancia interestelar y línea de visión euclidiana.

## Rutas y retardo

`FindPath` ejecuta A* mediante `NetworkPathfinder.Solve` para cada estación de
comando y estación terrestre. Pasa:

- coste: `RangeModelExtensions.DistanceTo(ISatellite, NetworkLink<ISatellite>)`;
- heurística: `RangeModelExtensions.DistanceTo(ISatellite, ISatellite)`.

Ambas funciones usan distancia euclidiana. La longitud de la ruta es la suma de
costes. `NetworkRoute.Delay` devuelve `Length / SpeedOfLight` cuando el retardo
está habilitado.

Por eso **no basta con insertar una arista**: sin parche de coste tendría longitud
interestelar y sin corregir la heurística A* dejaría de ser admisible.

## Punto exacto de inyección implementado

MVP 4 usa Harmony sobre el método privado
`NetworkManager.UpdateGraph(ISatellite)`:

- Prefix: retirar silenciosamente del listado del origen las aristas RTWB de la
  reconstrucción anterior;
- Postfix: añadir los `NetworkLink<ISatellite>` especiales vigentes para ese
  origen, con `LinkType.Dish` y la antena direccional real del origen en
  `Interfaces`.

El Postfix se ejecuta antes de que `OnPhysicsUpdate` llame a `FindPath`, por lo que
no existe una ventana en que se calculen rutas sin el enlace.

El controlador RTWB se instancia en Flight y Tracking Station. En ambas escenas
refresca el catálogo, endpoints y cobertura cada segundo; esto es necesario
porque RemoteTech ejecuta `UpdateGraph` y `FindPath` en las dos. Al salir limpia
el registro para no conservar referencias de satélite obsoletas.

RemoteTech 1.9.12 usa `RTCoreTracking` en Tracking Station y su
`AntennaWindowStandalone` asigna directamente `IAntenna.Target`. RTWB no parchea
esa UI: el siguiente refresco periódico observa el nuevo objetivo y reconstruye
los enlaces correspondientes.

La retirada previa evita que el original emita un `OnLinkRemove` falso en cada
reconstrucción. La arista se añade a ambos orígenes cuando corresponde y solo
después de validar cobertura bidireccional.

La inserción modifica directamente `Graph` y no emite `OnLinkAdd`; así el enlace
es consumible por rutas y módulos de RemoteTech sin generar todavía la línea
interestelar de su renderer.

## Coste topológico y heurística implementados

Se mantiene un registro externo por pareja ordenada de GUID con su distancia
efectiva. Dos Prefix de Harmony:

1. `RangeModelExtensions.DistanceTo(ISatellite, NetworkLink<ISatellite>)`
   devuelve `localA + 1000 m + localB` si la arista está registrada.
2. Durante `NetworkManager.FindPath`, un contexto acotado hace que el overload
   `DistanceTo(ISatellite, ISatellite)` usado como heurística devuelva cero.

Con heurística cero A* se convierte en Dijkstra: aumenta el coste de CPU, pero
garantiza rutas correctas en una topología no euclidiana. El contexto solo estará
activo dentro de `FindPath`, para no alterar los cálculos normales de alcance.
Se requiere Finalizer para limpiar el contexto incluso si hay excepción.

## Riesgos

- Harmony sobre métodos privados depende de firmas exactas; el bootstrap debe
  verificarlas y desactivar solo la integración si cambian.
- `NetworkLink.Equals` ignora tipo e interfaces; un enlace normal y uno RTWB a la
  misma nave no pueden coexistir limpiamente.
- La pérdida de la heurística puede ser perceptible en redes enormes.
- `ProtoAntenna` es interno. MVP 3 usa la interfaz pública `IAntenna` y limita la
  reflexión al campo `mProtoPart` para asociar la antena con su snapshot. La firma
  exacta se valida fuera de Unity; si cambia, solo se omiten endpoints descargados.
- La API pública de RemoteTech no ofrece inserción de aristas ni distancia
  personalizada; una API upstream sería más estable.
