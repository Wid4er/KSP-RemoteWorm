# Arquitectura propuesta

## Estado actual

Están implementados los registros, el escáner KEX, endpoints cargados y
descargados, `ModuleRTWormholeBridge`, geometría radial y cobertura
bidireccional. El parche Harmony sobre `WormholeComponent.MakeOrbit` conserva la
telemetría de regresión. MVP 4 ya inyecta aristas dirigidas y corrige coste y
heurística; el renderer sigue desactivado.

## Componentes del MVP

```text
KexWormholeScanner
  -> WormholeRegistry
  -> WormholePairDescriptor

RemoteTechEndpointScanner
  -> EndpointValidator
  -> WormholeEndpointRegistry

WormholeGeometryDiagnostics
  -> transformación verificada

WormholeLinkBuilder
  -> WormholeBridgeLinkRegistry
  -> RemoteTechGraphPatches
  -> RemoteTechPathCostPatches

WormholeBridgeLinkRegistry
  -> WormholeLinkRenderer
  -> WormholeConeRenderer
```

## Registro de agujeros

El adaptador recorre `PSystemManager.Instance.localBodies`, extrae
`WormholeComponent` por referencia directa y convierte sus datos a descriptores.
El núcleo ya valida:

- identificadores no vacíos;
- ausencia de duplicados por cuerpo;
- existencia del compañero;
- reciprocidad A↔B;
- parámetros de altura finitos y ordenados;
- SOI finita y mayor que la superficie de transición;
- banda operacional no degenerada completamente contenida en la SOI;
- deduplicación determinista de parejas.

No se acepta silenciosamente A→B si B no apunta a A.

## Registro de endpoints

El núcleo registra solo endpoints que cumplen las banderas verificables:

- nave RemoteTech real;
- antena direccional activa y alimentada;
- objetivo igual al agujero local;
- capacidad `ModuleRTWormholeBridge` habilitada;
- región operacional válida;
- alcance suficiente para el tramo entre relé y superficie lógica;
- canal no negativo.

El adaptador cargado asocia `IAntenna` con el `PartModule` porque
`ModuleRTAntenna` hereda de `PartModule`. Para naves descargadas usa la colección
pública `RTCore.Instance.Antennas[vessel]` y una reflexión acotada del campo
`ProtoAntenna.mProtoPart`, cuya firma se valida en el smoke test. El estado de
puente se lee del snapshot persistido, con el prefab parcheado como valor base.
La clave del endpoint combina GUID de nave y `flightID` de pieza; el GUID de
`IAntenna` no es único entre antenas hermanas.

## Fuente única de verdad

Cada `WormholeBodyDescriptor` contiene su `BridgeOperationalBand`, calculada a
partir de `sphereOfInfluence` y de la superficie de transición. Validación de
endpoints, logging, conos y anillos consumen esa misma instancia; no existen
constantes globales de distancia usadas directamente por esos subsistemas.

El registro runtime `RuntimeBridgeLink` almacena extremos y distancia efectiva a
través de sus endpoints. La telemetría conserva puntos de entrada/salida,
radiales, semianchos, errores angulares, distancias locales, canal y estado. El
inyector consume el registro validado y no recalcula geometría.

## Orden de ejecución

1. El bootstrap verifica assemblies y firmas.
2. Se refrescan parejas KEX y endpoints al iniciar escena y ante eventos
   significativos.
3. La geometría se recalcula antes del ciclo de grafo afectado.
4. Prefix/Postfix de `NetworkManager.UpdateGraph` sustituyen las aristas RTWB del
   origen actualizado.
5. Los parches de coste convierten A* en Dijkstra durante `FindPath` y aplican la
   distancia efectiva a la arista especial.
6. RemoteTech publica sus rutas y retardo normales.
7. El renderer propio dibuja tramos, conos y anillos de guía orbital.

## Degradación segura

- Falta KEX o RemoteTech: no se inicializa RTWB.
- Firma interna distinta: no se aplican parches de grafo; se registra un único
  error accionable.
- Cambio de transformación o firma interna: se desactiva la parte afectada y se
  registra un error accionable.
- Fallo del renderer: se conserva el enlace lógico.
- Fallo de inyección/coste: se eliminan datos visuales activos.

## Decisiones aplazadas

- UI y persistencia de ajustes globales;
- propuesta de API upstream para RemoteTech.
