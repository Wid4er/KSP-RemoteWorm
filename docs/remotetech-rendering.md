# Renderizado de RemoteTech 1.9.12

## Líneas

`NetworkRenderer` mantiene un `HashSet<BidirectionalEdge<ISatellite>> mEdges`.
Sus handlers privados `OnLinkAdd` y `OnLinkRemove` alimentan ese conjunto desde
los eventos de `NetworkManager`. En `OnPreCull`, `UpdateNetworkEdges` filtra con
`CheckVisibility`, crea `NetworkLine` y dibuja una única línea entre las
posiciones de A y B en ScaledSpace.

`BidirectionalEdge` solo guarda A, B y `LinkType`. No admite metadatos RTWB.

## Supresión exacta de la línea galáctica

La estrategia menos invasiva es **supresión por construcción**:

- el Postfix de `UpdateGraph` añade la arista directamente a `Graph`;
- no invoca el evento `OnLinkAdd` para esa arista;
- por tanto `NetworkRenderer.mEdges` nunca contiene la pareja RTWB y no puede
  construir una `NetworkLine` interestelar.

RTWB dibujará sus dos segmentos con un renderer propio que consuma exactamente
el mismo `WormholeBridgeLink` usado por el inyector.

Como defensa, si una versión futura decide emitir eventos, el punto de filtrado
es un Harmony Prefix sobre el método privado
`NetworkRenderer.OnLinkAdd(ISatellite,NetworkLink<ISatellite>)`: si la pareja está
en el registro RTWB se omite el original. No debe filtrarse `CheckVisibility`
salvo como último recurso, porque se evalúa cada frame y obliga a identificar la
arista sin metadatos.

## Conos normales

`UpdateNetworkCones` solo considera antenas alimentadas, orientables, con satélite
y objetivo. Respeta `NetworkRenderer.ShowCone`, derivado de `MapFilter.Cone`.

`NetworkCone` dibuja dos bordes, no un volumen. Usa:

- origen: posición de la nave de `dish.Guid`;
- centro: posición del objetivo;
- semiancho: `acos(dish.CosAngle)`;
- alcance gráfico: mínimo entre `dish.Dish` y la distancia al objetivo;
- material stock `Telemetry/TelemetryMaterial`;
- capa 31 y conversión `ScaledSpace.LocalToScaledSpace`.

## Sincronización propuesta de conos salientes

`WormholeConeRenderer` será propio y se actualizará en `OnPreCull` o componente
equivalente de la cámara de mapa. Su visibilidad mínima debe exigir:

```text
RTCore.Instance.Renderer != null
RTCore.Instance.Renderer.ShowCone
MapView.MapIsEnabled
endpoint válido o potencial
```

Para `SELECTED_ONLY`, la selección puede obtenerse de
`PlanetariumCamera.fetch.target.vessel`; no existe un filtro público de antena
seleccionada en el renderer.

La selección de una nave consumidora también muestra los dos segmentos locales
del puente concreto que aparece en alguna de sus rutas RemoteTech vigentes. Esto
no equivale a `ACTIVE_BRIDGES`: otros puentes activos permanecen ocultos.

Al seleccionar directamente un relé aceptado, los conos se calculan de forma
independiente de `RuntimeBridgeLink`: se muestran los de todos los endpoints
aceptados de ambas bocas de ese par, estén o no emparejados. De este modo son una
referencia previa de cobertura y no una confirmación tardía de un enlace ya
creado. La selección sólo activa el par correspondiente; no se dibujan de forma
permanente los conos de los demás agujeros.

En vuelo, la nave propietaria de la ruta es `FlightGlobals.ActiveVessel` y debe
conservarse aunque la cámara se enfoque sobre un cuerpo o agujero distante; usar
solo `MapCamera.target.vessel` hace imposible observar el tramo remoto.

El cono saliente usa `exitPoint`, dirección radial transformada y semiancho real.
Sus dos aristas se recortan por intersección con `InnerRadius` y `OuterRadius`
de `BridgeOperationalBand`, por lo que forman un cono truncado que solo ocupa la
banda donde un relé puede ser elegible. Debe reutilizar
material/capa/conversiones de `NetworkCone`, pero no su clase directamente:
`NetworkCone` asume que el origen es una antena registrada y que el eje termina
en un objetivo normal.

## Anillos de banda operacional

Cuando la nave relevante tiene una antena RTWB direccional, habilitada, activa,
alimentada y apuntada al agujero local, el renderer dibuja dos círculos rojos
semitransparentes:

```text
radio interior = transitionRadius + minimumLocalDistance
radio exterior = transitionRadius + maximumLocalDistance
```

No se exige que la nave ya esté dentro de la banda. Los círculos usan el plano
orbital obtenido de posición y velocidad evaluadas de la nave seleccionada. Si
ese plano degenera, usan el plano ecuatorial estable del cuerpo. Son mallas sin
collider, no objetos `Orbit`, y no modifican patched conics.

Cada círculo se aproxima mediante segmentos del mismo pool de `MapLineMesh`.
Las tablas trigonométricas se crean una sola vez y los `GameObject` solo se
añaden cuando el pool necesita crecer, no en cada frame. Su visibilidad sigue el
filtro Cone de RemoteTech.

## Riesgos de renderizado

- RemoteTech alterna líneas 2D/3D con `MapView.Draw3DLines`; el renderer propio
  debe cubrir ambos caminos.
- La geometría central de las aristas del cono se fija a la tangente ecuatorial
  del cuerpo de salida. La cámara solo determina el ancho en pantalla de cada
  línea y nunca la orientación de la apertura.
- Floating Origin obliga a calcular puntos desde posiciones actuales, sin guardar
  `Transform` persistentes.
- Si el renderer falla, el grafo debe seguir funcionando y los objetos deben
  limpiarse al cambiar de escena.
