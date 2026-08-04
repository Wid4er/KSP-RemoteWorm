# Prueba del enlace lógico y renderizado en KSP

## Instalación

Copiar esta carpeta completa:

```text
build/GameData/RemoteTechWormholeBridge
```

dentro del `GameData` de KSP, sustituyendo la versión anterior. No copiar
`build/inspection`.

Dependencias: KSP 1.12.5, RemoteTech, KEX/Kopernicus Expansion, Harmony y
ModuleManager. WormholeSignalBridge no es necesario.

## Preparación de los relés

Se necesitan dos naves con antena direccional RemoteTech:

1. Un relé alrededor de WH3141A, con la antena apuntando a **WH3141A**.
2. Otro relé alrededor de WH3141B, con la antena apuntando a **WH3141B**.
3. Ambas antenas activas, alimentadas y con el puente habilitado.
4. Ambas naves entre 100 y 300 km de distancia local respecto a la superficie
   de transición. En WH3141A/B equivale a una altitud de 135 a 335 km: la
   superficie lógica está a 45 km del centro y el cuerpo tiene 10 km de radio.
5. Ambas antenas en el mismo canal; el valor predeterminado es `0`.
6. Colocarlas con radiales similares. El error angular entre ambos radiales debe
   caber dentro del semiancho de cada plato.

No es necesario tener cargadas físicamente ambas naves: RTWB consulta también
las antenas proto de RemoteTech.

## Procedimiento

1. Arrancar KSP y cargar la partida.
2. Visitar/guardar una vez cada relé si fue creado antes de instalar RTWB.
3. Abrir la escena de vuelo de cualquiera de los dos y esperar unos segundos.
4. Comprobar que una nave cuya ruta de mando necesita cruzar el agujero recibe
   conexión RemoteTech y que el retardo no corresponde a la distancia
   interestelar entre WH3141A y WH3141B.
5. Desactivar o desalinear una de las antenas: la conexión debe desaparecer tras
   la siguiente actualización de red.
6. Reactivarla/alinearla y comprobar que la conexión vuelve.
7. Cerrar KSP y conservar el `KSP.log` completo.
8. Abrir el mapa, seleccionar uno de los relés elegibles y activar los filtros
   de enlaces direccionales y conos de RemoteTech. Deben verse los tramos locales
   de sus enlaces activos y los conos salientes magenta de todos los endpoints
   aceptados de ambas bocas, también los que no estén emparejados; nunca una
   línea entre sistemas.
9. Comprobar visualmente que cada cono nace en la superficie del agujero
   compañero y termina a 300 km de ella, independientemente del alcance del
   plato.
10. Con cuatro relés aceptados, dos por boca, comprobar que aparecen cuatro
    conos aunque solo dos parejas estén alineadas. Cada cono usa dos aristas.
11. Seleccionar una nave cuya ruta a KSC atraviese el puente. Deben permanecer
    visibles los dos segmentos locales magenta, pero no los conos de los relés.
    Enfocar después el agujero o su sistema: los segmentos deben seguir visibles
    aunque el objetivo de la cámara ya no sea la nave.
12. Entrar en Tracking Station, seleccionar una nave cuya ruta use el puente y
    confirmar que conserva conexión y segmentos. Seleccionar directamente un
    relé debe mostrar los conos de su pareja de agujeros.
13. Desde Tracking Station, cambiar el objetivo de una antena puente: el endpoint
    y su enlace deben retirarse o reconstruirse en aproximadamente un segundo,
    sin tener que entrar en vuelo.

## Resultado esperado

```text
[RTWB] version plugin=0.4.0-render-test ...
[RTWB] Harmony network patches applied updateGraph=UpdateGraph findPath=FindPath ...
[RTWB] renderer-attached color=#FF4FD8 operationalBand=100000-300000 coneLength=300000 ...
[RTWB] mode=logical-link graphMutation=True renderer=True scene=FLIGHT|TRACKSTATION ...
[RTWB] endpoint-scan reason=flight-start|tracking-start ... candidates=... accepted=2 ...
[RTWB] bridge-scan ... eligiblePairs=1 active=1 ... graphMutation=True
[RTWB] bridge-coverage ... validAB=True ... validBA=True active=True ...
[RTWB] graph-edge-injected source=... target=... effectiveDistance=... rendererEvent=false
[RTWB] path-cost-overridden source=... target=... effectiveDistance=...
[RTWB] path-bridge-routes start=... count=... goalLengthDelay=...
[RTWB] renderer-visible links=... coneEndpoints=... meshes=... segments=True cones=... selection=endpoint|route selected=... coneLength=300000
```

`active=True` confirma la geometría; `graph-edge-injected` confirma la arista;
`path-cost-overridden` confirma el coste no euclidiano; y `path-bridge-routes`
demuestra que al menos una ruta publicada por RemoteTech atraviesa el puente.
Los campos después del destino son longitud y retardo de la ruta.

No debe aparecer una línea visual que una ambos sistemas: los eventos
`OnLinkAdd`/`OnLinkRemove` siguen omitiéndose y el renderer propio solo dibuja
los tramos locales.

Si un endpoint es rechazado, `endpoint-rejected` informa el motivo: objetivo
incorrecto, antena inactiva, falta de energía, distancia inferior a 100 km,
distancia superior a 300 km, alcance local insuficiente o canal inválido.

Para extraer la telemetría:

```bash
rg '\[RTWB\]' KSP.log
```

La telemetría de salto (`jump-snapshot` y `jump-transform`) permanece habilitada
como comprobación de regresión de KEX.

## Resultado verificado

La prueba del 2026-08-04 activó la pareja con un error angular de `7.95038875°`.
RTWB inyectó A→B y B→A, sustituyó ambos costes por aproximadamente `511 km` y
RemoteTech publicó una ruta hacia KSC que contiene la arista especial. El usuario
confirmó conexión funcional en juego; no se registraron errores RTWB.
