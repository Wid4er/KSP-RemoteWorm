# Estado verificado de RTWB

Actualizado: 2026-08-11.

## Completado

- `TASK.md` leído completamente.
- MVP 0 documentado en `docs/`.
- RemoteTech suministrado coincide bit a bit con la release oficial 1.9.12.
- WormholeSignalBridge suministrado coincide bit a bit con el paquete v2.0.0.
- La lógica KEX inspeccionada cambia `orbit.referenceBody` y llama a
  `updateFromParameters`; no contiene una transformación cartesiana explícita.
- Núcleo .NET 4.8 de registros de agujeros/endpoints compilado y probado.
- Localizada la instalación Steam de KSP, build `03190`, con las referencias
  reales necesarias: Managed de KSP, RemoteTech, KEX, Kopernicus y Harmony.
- Verificadas las versiones de assembly de las dependencias principales:
  RemoteTech `1.9.0.0`, KEX-Wormholes `1.0.0.0`, Kopernicus `1.0.247.0` y
  Harmony `2.2.1.0`.
- Los saltos A→B y B→A verificaron la transformación radial orbital identidad:
  errores `0°` y `8.5377364625159387E-07°`, con elementos conservados.
- Creado el plugin diagnóstico `0.2.1`: descubre endpoints RemoteTech cargados y
  descargados, calcula geometría de transición y cobertura bidireccional.
- Corregido el parche ModuleManager para ejecutarse `:AFTER[RemoteTech]`; la
  versión anterior se evaluaba antes de que RemoteTech añadiera sus antenas y
  producía `candidates=0`.
- Corregida la suscripción SOI: `EventData` de KSP no acepta el delegado estático
  usado por `0.1.0`; ahora el manejador pertenece a la instancia del controlador.
- La primera prueba de cobertura detectó los dos extremos, incluido el descargado,
  y una pareja elegible. Quedó inactiva correctamente: separación radial
  `107.36505415196649°` frente a semianchos de `45°` en ambos platos.
- El mismo log verificó que `IAntenna.Guid` es el GUID de nave y colisiona entre
  antenas hermanas. `0.2.1` usa el `flightID` persistente de la pieza y tiene una
  prueba para conservar por separado antenas válidas y rechazadas de una nave.
- La ejecución de `0.2.1` verificó las identidades por pieza: seis antenas,
  exactamente dos endpoints aceptados y cuatro rechazos independientes. No hubo
  excepciones RTWB. La pareja empezó con `75.03°` de separación y no cubrió los
  semiconos de `45°`.
- `Satelite Bujero` sigue en una órbita `SMA≈165 km`, `e≈0.818`, con periapsis
  aproximado de 20 km de altitud; por eso vuelve a saltar antes de mantener una
  alineación segura. Debe elevar el periapsis por encima de 35 km antes de cerrar
  la validación de cobertura.
- Después de la maniobra, el guardado confirma dos órbitas estables casi
  circulares (`SMA≈300 km`) alrededor de WH3141A/B. La pareja queda operativa
  salvo por fase: separación `177.39383808769236°` frente a semiconos de `45°`.
  Una órbita de fase con apoapsis 290 km de altitud y periapsis 50 km debe dejar
  aproximadamente 10-15° de separación al regresar al apoapsis.
- MVP 3 quedó validado dentro de KSP: RTWB registró `active=True` con dos
  endpoints reales, cargado/descargado, `validAB=True` y `validBA=True`. Hubo
  mediciones activas a `34.563240416940744°`, `1.0927074607944676°` y
  `1.3663476484630943°`, dentro de semiconos de `45°`, sin excepciones RTWB.
- Cambios de SOI posteriores dejaron el último estado a `173.793°` e inactivo;
  esto es una variación de la geometría de la partida, no un fallo de detección.
- El parche Harmony de salto sigue siendo solo diagnóstico. La integración de
  red usa parches separados y degradables con Harmony 2.2.1.
- El paquete instalable se genera en `build/GameData/RemoteTechWormholeBridge`.
- Implementado MVP 4 en `0.3.0-logical-link-test`: inyección dirigida A↔B tras
  `NetworkManager.UpdateGraph`, retirada silenciosa antes de la reconstrucción,
  coste efectivo `localA + 1000 m + localB` y heurística cero solo dentro de
  `FindPath`. No se emiten eventos del renderer.
- El smoke test comprueba las firmas exactas de RemoteTech 1.9.12 y que Harmony
  acepta Prefix/Postfix/Finalizer equivalentes. Build Release y pruebas core
  pasan.
- MVP 4 quedó validado dentro de KSP con `0.3.0-logical-link-test`: cobertura
  bidireccional activa a `7.9503887535133009°`, dos aristas dirigidas inyectadas
  y coste especial de `511000.0000017776 m` aplicado en ambos sentidos.
- RemoteTech publicó una ruta real que atraviesa el puente hacia KSC
  (`5105f5a9-d628-41c6-ad4b-21154e8fc488`), con longitud
  `73752201141.642471 m` y retardo `245.84067047214157 s`. El usuario confirmó
  en juego que la conexión funciona y no hubo errores RTWB en el log.
- Implementada y validada dentro de KSP `0.4.0-render-test`: banda operativa
  inclusiva de 100-300 km desde la superficie de transición, dos segmentos
  locales magenta `#FF4FD8` y conos salientes magenta de 300 km.
- El renderer propio se ejecuta en la cámara de mapa y respeta los filtros Dish
  y Cone de RemoteTech. Muestra el puente del relé seleccionado o los segmentos
  del puente concreto usado por la ruta de la nave seleccionada; los conos siguen
  reservados para la selección directa de un relé.
- Corregido antes de validar `0.4.0`: los vectores de `Orbit` deben convertirse
  al mundo como `(x,z,y)` antes de sumarlos a `body.position`; sin ese swizzle,
  un cono ecuatorial se dibujaba en posición polar.
- Corregida la orientación de las aristas del cono: su apertura usa la tangente
  ecuatorial del cuerpo de salida (`eje polar × radial`) y ya no depende de la
  posición de la cámara. El usuario confirmó visualmente origen, orientación y
  estabilidad correctos.
- La ejecución limpia de validación cargó exactamente la DLL compilada, mantuvo
  `active=True` a `7.950461°`, inyectó ambas aristas, publicó rutas hacia KSC y
  mostró seis mallas (`segments=True`, `cones=True`). No hubo errores ni
  excepciones de RTWB o RemoteTech.
- Corregida tras la validación la visibilidad desde consumidores: `Suluco Prueba`
  tenía una ruta RemoteTech que atravesaba Bujero B→A, pero el filtro solo
  comparaba la selección con los endpoints. Ahora cada puente se muestra si su
  arista aparece en una ruta de la nave seleccionada, en cualquiera de los dos
  sentidos.
- La primera prueba de esa corrección cargó la DLL adecuada y registró
  `meshes=2 segments=True cones=False selection=route selected=Suluco Prueba`,
  pero las líneas desaparecían al enfocar el agujero: el renderer confundía la
  nave propietaria de la ruta con el objetivo de la cámara. Ahora conserva la
  nave activa/de ruta aunque se enfoque otro objeto del mapa; el foco solo
  controla la selección directa de endpoints y conos. Compilado y probado;
  pendiente de confirmación dentro de KSP.
- La prueba con dos relés por extremo en posiciones opuestas validó el
  emparejamiento múltiple: cuatro combinaciones elegibles, dos activas a
  `7.948°` y `9.726°`, y dos cruzadas rechazadas a `162.689°` y `179.637°`.
  Las cuatro aristas dirigidas correspondientes a las dos parejas válidas se
  inyectaron con su coste especial.
- Esa prueba descubrió un defecto transitorio de robustez al registrar/descargar
  naves: `RuntimeBridgeLink.SourceGuid` consulta el `Guid` de un
  `VesselSatellite` obsoleto cuyo `SignalProcessors` ya está vacío, provocando
  `ArgumentOutOfRangeException` en `Outgoing`. El parche falla abierto y las
  rutas se recuperan al reconstruirse RemoteTech.
- Corregido ese defecto capturando `SourceGuid` y `TargetGuid` desde `Vessel.id`
  al crear cada enlace. El smoke test exige ahora campos GUID estables y el
  build, las pruebas core, el smoke test y `pedump` pasan; falta confirmar en KSP
  que no reaparece la excepción durante el alta/baja de naves.
- Los conos ya no dependen de parejas activas. Al seleccionar un relé aceptado
  en la banda de 100-300 km, el renderer muestra los conos de todos los endpoints
  aceptados de ambas bocas del par, estén o no emparejados. Las líneas magenta
  continúan reservadas para enlaces activos. Compilado y probado fuera de KSP;
  pendiente de validación visual.
- Validado dentro de KSP el comportamiento dinámico: cuatro conos simultáneos,
  retirada y recuperación de rutas, desactivación de antena, cambio de objetivo
  y filtros Dish/Cone sin afectar al grafo. No reapareció la excepción de GUID
  obsoleto durante descargas y registros masivos de RemoteTech.
- Detectado y corregido soporte ausente en Tracking Station: el controlador era
  exclusivo de `Startup.Flight` y limpiaba todos los enlaces al salir. Ahora se
  instancia con `Startup.EveryScene`, se habilita únicamente en Flight y
  Tracking Station, usa `tracking-start` y selecciona la nave enfocada en esa
  escena. Validado dentro de KSP: `scene=TRACKSTATION`, dos aristas inyectadas,
  rutas publicadas para relé y consumidor, segmentos renderizados y cambios de
  objetivo funcionales, sin excepciones RTWB.
- Validada pérdida real de energía bajo time warp: un nuevo relé aceptado creó
  sus aristas, pasó a `powered=False`, salió del registro y perdió su ruta sin
  afectar al otro puente activo ni generar excepciones RTWB. RemoteTech pone
  `Dish=0` en ese estado. Corregida la prioridad diagnóstica para informar
  `Inactive`/`Unpowered` antes de `NotDirectional`, con prueba de regresión.
- Validado docking dentro de KSP: RemoteTech fusionó el GUID de la nave auxiliar
  con el relé superviviente y RTWB reescaneó el endpoint en aproximadamente un
  segundo, sin aristas obsoletas ni excepciones RTWB. El usuario confirmó que el
  undocking ya funcionaba correctamente.
- Validada destrucción desde Tracking Station: al eliminar `Bujero Docking`, los
  endpoints aceptados bajaron de tres a dos y las parejas activas de dos a una;
  el puente restante continuó operativo y no hubo excepciones RTWB.
- Confirmado el modelo multirrelé: RTWB publica todas las aristas compatibles y
  RemoteTech escoge la ruta completa por coste para cada origen. Los canales son
  una partición manual opcional y no se expondrán en la interfaz mientras no
  exista una necesidad de juego concreta.
- El propietario del proyecto autorizó el 2026-08-05 distribuir RTWB bajo GNU
  GPL v3.0 only. Preparada la versión `0.5.0-beta.1`; las DLL de terceros siguen
  siendo dependencias externas y no forman parte del paquete.
- `0.5.0-beta.1` compila Release con cero avisos y errores; pasan las pruebas
  core, el smoke test de Harmony y `pedump`. El ZIP instalable contiene solo
  archivos propios y su SHA-256 es
  `d7e9d4cfc6771bef97b6822e165a5ef7f19e413b70050e2c114df392ab7e29c7`.
- Publicada la prerelease `v0.5.0-beta.1`; el tag permanece en el commit binario
  `bccd830b504033682863e01b1caf83b156f72a6e`. El README público quedó en inglés,
  reconoce a WormholeSignalBridge como inspiración y acredita la asistencia de
  OpenAI Codex.
- Los cuatro wormholes opcionales de Promised Worlds suministrados usan radio de
  10 km, SOI de 80 km, `influenceAltitude=35 km`, `jumpMaxAltitude=30 km` y
  `jumpMinAltitude=10 m`. Singularity solo aporta su representación visual; KEX
  ejecuta el salto. Promised Worlds los deshabilita por defecto y requiere
  `Wormholes=True` para probarlos.
- Implementada una banda operacional inmutable por wormhole. Calcula espacio
  disponible como `SOI - transitionRadius`, máximo como
  `min(300 km, available*0.8)` y mínimo como `max(5 km, maximum/3)`. Una SOI de
  80 km con transición a 45 km produce aproximadamente 9,33-28 km; una SOI
  amplia produce matemáticamente 100-300 km sin excepciones por planet pack.
- La validación, scanner, logging y conos consumen la misma banda del descriptor.
  SOI no finitas, negativas, inferiores a la transición o que produzcan bandas
  degeneradas se rechazan sin modificar datos externos.
- Implementados dos anillos rojos `#FF3030` semitransparentes en el plano orbital de la
  nave seleccionada, con fallback ecuatorial estable. Se muestran con el filtro
  Cone para una antena direccional RTWB activa, alimentada y apuntada al agujero,
  aunque la nave esté fuera de banda. Reutilizan el pool de `MapLineMesh`.
- El cambio de banda dinámica y anillos compila Release con cero avisos y
  errores; pasan pruebas core de SOI grande/media/pequeña/inválida, smoke test de
  Harmony aislado y `pedump --verify all`.
- Validado dentro de KSP con los wormholes de Kcalbeloh: el usuario confirmó que
  el enlace sigue funcionando perfectamente y que ambos anillos se ven y se
  comportan correctamente.
- Validada dentro de KSP la banda comprimida de Promised Worlds. El log detecta
  las bocas Kevbas A/B con SOI de 80 km y banda `9,333-28 km`; acepta relés a
  aproximadamente 15,47 y 25 km, mantiene cobertura bidireccional activa con
  error angular cercano a 3 grados frente a 45 grados de semicono, inyecta ambas
  aristas en el grafo y registra una ruta real de RemoteTech desde Kevbas B hasta
  KSC. No aparece ninguna excepción ni pila de llamadas de RTWB.
- Esa sesión usó la compilación anterior al cambio visual de los anillos a rojo:
  el log aún muestra `renderer-attached color=#FF4FD8` y no los campos separados
  `bridgeColor`/`guideColor`. La lógica y el render de anillos quedaron validados,
  pero el rojo `#FF3030` solo requiere una comprobación visual breve con la DLL
  Release más reciente.

## Decisiones vigentes

- WSB es referencia arquitectónica, no API ni autoridad geométrica para RTWB.
- La transformación es identidad sobre radiales orbitales normalizados.
- La cobertura real ya autorizó la implementación del enlace lógico.
- RemoteTech consume el enlace lógico y MVP 5/6 quedó validado visualmente dentro
  del juego.
- La integración de grafo se engancha alrededor de
  `NetworkManager.UpdateGraph(ISatellite)` y requiere coste topológico más
  heurística cero durante `FindPath`.

## Siguiente trabajo

1. Validar una instalación limpia del ZIP publicado `0.6.0` junto con
   RemoteTech Overhaul `0.1.0`.

## Bloqueos/riesgos

- Las DLL no están en `ayuda`, pero se localizaron en la instalación Steam y
  pueden referenciarse mediante una propiedad de build sin copiarlas al paquete.
- El soporte descargado depende de la firma interna `ProtoAntenna.mProtoPart`;
  el smoke test la verifica y el runtime degrada omitiendo solo esos endpoints.
- La autorización GPLv3 del proyecto queda registrada, pero RTWB sigue
  dependiendo de APIs de terceros y debe respetar sus avisos y condiciones.
- RemoteTech 1.9.12 degrada `Vessel.GetWorldPos3D()` de `Vector3d` a
  `UnityEngine.Vector3` dentro de cada `ISignalProcessor.Position`, y después
  vuelve a convertirlo a `Vector3d` en `VesselSatellite.Position`. A distancias
  interestelares esto pierde kilómetros de resolución. La corrección general se
  ha extraído a RemoteTech Overhaul; este proyecto la declara como dependencia
  y ya no parchea por si mismo el getter de RemoteTech.
- Retirados los diagnósticos temporales de auditoría del grafo
  (`path-bridge-state`, vecinos, aristas y secuencias completas de saltos). Se
  conserva únicamente el resumen operativo original `path-bridge-routes`.
- Las aristas visuales de los conos se recortan mediante intersección con
  `BridgeOperationalBand.InnerRadius` y `OuterRadius`. El resultado es un cono
  truncado que empieza y termina exactamente en los límites radiales elegibles,
  sin cambiar la geometría de cobertura ni el grafo.
- RemoteTech Overhaul `v0.1.0` está publicado como dependencia independiente en
  `Wid4er/KSP-RemoteTechOverhaul`. La release estable RTWB `0.6.0` usa esa
  dependencia, reemplaza el aviso beta por documentación de instalación y se
  empaqueta únicamente bajo `GameData/RemoteTechWormholeBridge`.
