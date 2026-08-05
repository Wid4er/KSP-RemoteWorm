# RemoteTech Wormhole Bridge (RTWB)

**Mod beta para Kerbal Space Program 1, versión 1.12.5.**

RemoteTech Wormhole Bridge permite que las señales de **RemoteTech atraviesen
los agujeros de gusano de Kopernicus Expansion (KEX)**. Así puedes mantener una
red de comunicaciones entre sistemas estelares sin necesitar antenas con
alcance de años luz ni sufrir un retardo interestelar absurdo.

Versión actual: **0.5.0-beta.1**.

## Para qué sirve

Sin RTWB, RemoteTech intenta comunicar las naves usando la distancia real entre
los sistemas. Aunque una nave haya cruzado un agujero de gusano, su señal no
puede aprovecharlo.

RTWB permite:

- construir relés de RemoteTech alrededor de cada boca;
- enviar comunicaciones a través del agujero de gusano;
- dejar que RemoteTech elija automáticamente la mejor ruta desde cada nave;
- conservar el control remoto, las rutas y el retardo de señal de RemoteTech;
- ver en el mapa los enlaces activos y las posibles zonas de cobertura;
- redefinir los objetivos de las antenas tanto en vuelo como desde el Tracking
  Station.

No tienes que emparejar manualmente dos relés concretos. RTWB detecta todas las
salidas compatibles y RemoteTech escoge la ruta más conveniente para el origen
de cada señal.

## Dependencias

Necesitas tener instalados:

- **Kerbal Space Program 1.12.5**;
- **RemoteTech 1.9.12**;
- **Kopernicus Expansion Continued-er**, incluido `KEX-Wormholes`;
- las dependencias de KEX, entre ellas **Kopernicus**;
- **HarmonyKSP**;
- **ModuleManager**.

**WormholeSignalBridge no es una dependencia.** Puede coexistir con RTWB, pero
está destinado a CommNet/RealAntennas y no interviene en la red de RemoteTech.

## Descargar e instalar

1. Descarga el ZIP desde la
   [página de Releases](https://github.com/Wid4er/KSP-RemoteWorm/releases).
2. Extrae su contenido directamente en la carpeta raíz de KSP.
3. Comprueba que el plugin quede en:

```text
Kerbal Space Program/
└── GameData/
    └── RemoteTechWormholeBridge/
        └── Plugins/
            └── RemoteTechWormholeBridge.dll
```

El ZIP solo contiene RTWB. No incluye KSP ni las DLL de sus dependencias.

## Cómo establecer un enlace

1. Coloca un relé con una antena direccional de RemoteTech alrededor de cada
   boca del mismo agujero de gusano.
2. Apunta cada antena hacia el cuerpo del agujero local.
3. Mantén las antenas activas y con energía.
4. Sitúa cada relé entre **100 y 300 km de la superficie de transición** de KEX.
5. Coloca los relés en regiones compatibles de ambas bocas para que uno quede
   dentro del cono proyectado por el otro.

En los agujeros `WH3141A` y `WH3141B` de Kcalbeloh System, la banda válida
equivale aproximadamente a una altitud de **135 a 335 km** sobre el cuerpo.

Cuando la geometría sea válida, el enlace aparecerá automáticamente y
RemoteTech podrá incluirlo en sus rutas. Si existen varios relés compatibles,
RemoteTech elegirá el recorrido completo de menor coste para cada nave.

## Indicadores visuales

En la vista de mapa o en el Tracking Station, activa los filtros de platos y
conos de RemoteTech:

- las **líneas magenta** representan enlaces de agujero activos;
- los **conos magenta** muestran las zonas en las que podría situarse un relé
  compatible;
- cada cono termina a 300 km de la superficie de transición;
- nunca se dibuja una línea atravesando la distancia interestelar entre ambos
  sistemas.

## Estado de la beta

Esta versión se ha probado con KSP 1.12.5, RemoteTech 1.9.12 y
KEX-Wormholes 1.0. Incluye soporte verificado para:

- relés cargados y descargados;
- vuelo y Tracking Station;
- pérdida y recuperación de energía;
- cambio de objetivo de antena;
- docking, undocking y destrucción de naves;
- varios relés alrededor de una misma pareja de bocas.

Al tratarse de una beta, se recomienda conservar una copia de la partida antes
de instalarla.

## Documentación técnica

La arquitectura, las APIs inspeccionadas, el proceso de compilación y las
pruebas de desarrollo están documentados en [docs/](docs/).

## Licencia

RTWB se distribuye bajo [GNU GPL v3.0 only](LICENSE). Las DLL de KSP, RemoteTech,
KEX y Harmony son dependencias externas y no se redistribuyen con este proyecto.
