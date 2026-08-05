# Compatibilidad

## Matriz verificada

| Componente | Estado |
|---|---|
| KSP 1.12.5 | build `03190`; binarios Managed verificados |
| RemoteTech 1.9.12 | DLL exacta verificada contra release oficial |
| KEX-Wormholes 1.0 | DLL instalada `1.0.0.0` y API exacta verificadas |
| WormholeSignalBridge 2.0.0 | referencia inspeccionada; no es dependencia de RTWB |
| ModuleManager 4.2.3 | DLL verificada |
| Singularity 0.994.9632.26920 | irrelevante para lógica; solo visual del agujero |
| Harmony | DLL instalada `2.2.1.0`; parches validados fuera de Unity y dentro de KSP |

RemoteTech 1.9.12 declara compatibilidad hasta KSP 1.12.1 en su `.version`, aunque
el proyecto objetivo usa KSP 1.12.5. Esto es un riesgo de metadatos/soporte que
debe validarse con una partida de prueba; no implica por sí solo incompatibilidad
binaria.

## Modos de fallo aceptables

- Sin RemoteTech: RTWB se desactiva por completo.
- Sin KEX: no se registran agujeros ni endpoints.
- Sin Harmony: MVP de descubrimiento/diagnóstico puede funcionar, pero no se
  inyectan enlaces.
- Con WSB instalado: ambos mods pueden descubrir KEX, pero RTWB no toca CommNet o
  RealAntennas. Deben usar namespaces, escenarios y objetos propios.
- Con CommNet stock: RemoteTech lo desactiva en `RTCore.Start`; RTWB no debe
  reactivarlo.
- Con Singularity ausente: la geometría lógica puede existir aunque falte el
  efecto visual del planet pack.

## Compatibilidad de guardado

Los enlaces se reconstruirán. Solo se persistirán campos normales del futuro
`ModuleRTWormholeBridge` (habilitación/canal) y ajustes. Eliminar RTWB no debe
corromper naves: KSP ignorará el PartModule desconocido, y RemoteTech conservará
su configuración original.

## Pruebas mínimas antes de declarar una combinación soportada

1. Arranque y cambio de escenas sin excepciones.
2. Descubrimiento recíproco de WH3141A/B.
3. Telemetría radial antes/después de un salto KEX.
4. Endpoint cargado y descargado.
5. Activación, energía y cambio de objetivo.
6. Ruta y retardo con/sin enlace.
7. Ausencia de línea interestelar.
8. Time warp, docking, undocking, destrucción y recarga de partida.

RTWB se distribuye bajo GNU GPL v3.0 only con autorización del propietario del
proyecto confirmada el 2026-08-05. RemoteTech, KEX, Harmony y las DLL de KSP son
dependencias externas: se referencian durante la compilación y no se incluyen en
el paquete. WormholeSignalBridge no es una dependencia.
