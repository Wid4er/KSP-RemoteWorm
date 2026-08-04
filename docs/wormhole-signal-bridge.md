# WormholeSignalBridge 2.0.0

## Alcance real

WSB depende directamente de RealAntennas 2.6.0 y KEX-Wormholes 1.0.0. No integra
RemoteTech. Su bootstrap espera `RACommNetScenario.RACN`, se suscribe a
`OnNetworkPreUpdate` y `NetworkUpdateComplete`, y llama a `RACommNetwork.MakeLink`
después de la reconstrucción normal.

## Arquitectura útil

- `DependencyChecker`: degradación segura por assembly name.
- `WormholeRegistry`: recorre cuerpos, obtiene `WormholeComponent`, registra
  parámetros y deduplica parejas.
- `WormholeMouthNodeManager`: crea y elimina nodos proxy siguiendo el ciclo de
  red.
- `WormholeLinkBuilder`: agrupa candidatos por cuerpo y prueba parejas.
- `WormholeLinkCalculator`: separa selección, presupuesto de cada sentido y
  diagnóstico.
- configuración mediante `GameParameters.CustomParameterNode`.

Estas ideas justifican separar en RTWB descubrimiento, validación, enlace,
inyección y presentación.

## Supuestos que no se reutilizarán

WSB crea para cada agujero un `RACommNode` y antenas proxy de todas las bandas.
Sitúa el nodo en un punto fijo de latitud/longitud orientado al cuerpo padre. Los
enlaces se calculan mediante pérdidas, bandas, diámetro, symbol rate y métricas de
RealAntennas, y se inyectan con `RACommNetwork.MakeLink`.

RTWB no debe copiar:

- nodos CommNet/RA;
- `RACommNetwork.MakeLink`;
- presupuestos RF de RealAntennas;
- objetivos `BodyLatLonAlt` ni el punto de boca fijo;
- reglas de bandas o symbol rate;
- eventos de reconstrucción de RA.

RemoteTech ya tiene su propia semántica de objetivo, cono, alcance, grafo, rutas y
retardo. La lógica útil de WSB es organizativa, no una API intercambiable.

## Licencia

WSB está bajo MIT (copyright Aebestach, 2026). Se puede adaptar código conservando
el aviso. En el núcleo actual no se ha copiado código: el registro fue escrito de
forma independiente y usa identificadores de dominio neutrales.

KEX está bajo GPL-3.0 y RemoteTech bajo GPL-2.0 sin que el fichero local indique
“or later”. Si el enlace dinámico se considerase una obra combinada, esas
licencias podrían ser incompatibles. Es un **riesgo legal pendiente**, no una
conclusión técnica: antes de distribuir RTWB hay que confirmar las declaraciones
de licencia de los proyectos y escoger una estrategia revisada (permiso de los
autores, API/proceso desacoplado u otra solución válida). La reflexión por sí sola
no debe asumirse como remedio de licencia.
