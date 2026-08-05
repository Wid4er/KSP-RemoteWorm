# RemoteTech Wormhole Bridge (RTWB)

Estado actual: **0.5.0-beta.1; puente funcional validado en KSP 1.12.5**.

Este repositorio contiene la documentación verificada de KEX, RemoteTech y
WormholeSignalBridge, el núcleo de dominio y una DLL instalable en fase beta.
La DLL descubre parejas KEX y endpoints RemoteTech cargados o descargados,
calcula puntos de transición, valida cobertura direccional bidireccional e
inyecta una arista dirigida por sentido en el grafo de RemoteTech. La
transformación radial identidad fue medida en saltos A→B y B→A. Los relés son
elegibles entre 100 y 300 km de la superficie de transición.

La arista usa como coste `distancia local A + 1 km + distancia local B`; durante
el pathfinding la heurística se anula para conservar la corrección de A*. El
renderer propio sustituye la cuerda interestelar por dos segmentos locales
magenta y proyecta en el extremo compañero los conos salientes, también
magenta, limitados a 300 km desde la superficie de transición. Al seleccionar
un relé elegible muestra los conos de todos los endpoints aceptados de las dos
bocas, aunque todavía no formen un enlace; las líneas siguen representando solo
enlaces activos.

El descubrimiento, la inyección de red y el renderer funcionan tanto en vuelo
como en Tracking Station. Los cambios de objetivo realizados desde la interfaz
de antenas de RemoteTech se reflejan durante el refresco periódico. Docking,
undocking, destrucción de naves y pérdida de energía reconstruyen o retiran las
aristas sin conservar enlaces obsoletos.

La regla del proyecto es deliberada: WormholeSignalBridge es una referencia de
arquitectura y descubrimiento, no una capa de RemoteTech ni la fuente de verdad
para la geometría.

## Compilar

```bash
KSP_ROOT="/ruta/a/Kerbal Space Program" \
  msbuild RemoteTechWormholeBridge.sln /t:Rebuild /p:Configuration=Release
mono build/inspection/RemoteTechWormholeBridge.Core.Tests.exe
```

El paquete instalable queda en:

```text
build/GameData/RemoteTechWormholeBridge/
```

Las DLL de KSP y de terceros se usan como referencias con copia local
desactivada y no se incluyen en el paquete.

## Prueba en juego

Instala la carpeta de salida dentro de `GameData`, configura dos relés y busca
las líneas `[RTWB] graph-edge-injected`, `[RTWB] path-cost-overridden`,
`[RTWB] path-bridge-routes` y `[RTWB] renderer-visible` en `KSP.log`.
Véase [docs/runtime-test.md](docs/runtime-test.md) para el procedimiento exacto.

WormholeSignalBridge no es una dependencia y puede permanecer instalado para
una prueba de coexistencia.

## Licencia

RTWB se distribuye bajo [GNU GPL v3.0 only](LICENSE). Las DLL de KSP, RemoteTech,
KEX y Harmony son dependencias externas y no se redistribuyen con este proyecto.
