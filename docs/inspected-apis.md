# APIs y artefactos inspeccionados

## Alcance de la evidencia

La carpeta suministrada se llama `ayuda`, no `dependencias`. Contiene 2,0 GiB y
3.194 archivos: 2.803 de `Squad`, 932 de `SquadExpansion`, 169 de
`KcalbelohSystem`, 109 de `RemoteTech`, 7 de `WormholeSignalBridge`, 5 de
`Singularity` y la DLL de ModuleManager.

Los assets de piezas, audio y texturas se inventariaron, pero no intervienen en
las APIs del puente. Se inspeccionaron en detalle las configuraciones de
agujeros, los ensamblados administrados y sus fuentes oficiales coincidentes.

## Versiones verificadas

| Componente | Evidencia | Resultado |
|---|---|---|
| RemoteTech | `RemoteTech.version`, manifiesto y hash de la release oficial | 1.9.12; assembly 1.9.0.0; SHA-256 `64aedaba7e8c0488d83d052ba945e163c650292bf61c5fa40cd329b72e4fffa1` |
| WormholeSignalBridge | `.version`, assembly y paquete v2.0.0 | 2.0.0; SHA-256 `98e640511554f37a21d8962b3fa59a875912a4ce89a1a36569e744ff04964fde` |
| ModuleManager | nombre y assembly | 4.2.3.0; SHA-256 `95847827ab293b9e82a19b7efb97ffea3e98cc03b181862536ac8eb572e30d7d` |
| Singularity | assembly | 0.994.9632.26920; SHA-256 `d6a8f7bec91c4961408682d0a89e418c60db316f246895f2ff0939f4bccf0b71` |
| Kcalbeloh System | configs suministradas | la pareja WH3141A/B coincide con el commit oficial `0c4e3e7`; no hay fichero de versión local para atribuir una release exacta |
| KEX-Wormholes | fuente oficial y DLL instalada | assembly `KEX-Wormholes` 1.0.0.0; SHA-256 `7f1f2f21f3b0d247d33fafd5c7352b4109cf4d542ee8ba8deaff84e913e5e31c` |
| KSP | instalación Steam | build `03190`; `Assembly-CSharp.dll` SHA-256 `d9e42483f25ee80a9c11d6c1c0a0d29b4ec78c1e08d76c971b71580c9cce51e4` |
| Harmony | DLL instalada | assembly 2.2.1.0; SHA-256 `19ce60ac3280f72ec1751d36a40cb7e2fece2934df8345969dc7feb83bd633e4` |

## Fuentes primarias contrastadas

- RemoteTech: release 1.9.12, commit `5cdd8654da005ac7c52b9679e0fd4938b0235064`.
- WormholeSignalBridge: tag v2.0.0, commit `445c4c2403fb356206b368039f6ec99930509f5f`.
- KEX: `StollD/KopernicusExpansion-Continued`, commit inspeccionado
  `5c219cd`; `WormholeComponent.cs` y `WormholeLoader.cs`.
- Kcalbeloh System: commit `0c4e3e7b4b10f037030cc638104261675eac1f50`.

La DLL de WSB lleva el commit informativo `422e2c0...`; entre ese commit y el tag
solo cambian alias de namespace para desambiguar `RealAntennas.Physics`. El
comportamiento relevante no cambia y el binario suministrado coincide con el del
tag.

## APIs verificadas de KEX

`KopernicusExpansion.Wormholes.WormholeComponent` es público y expone:

- `jumpMaxAltitude`, `jumpMinAltitude`, `influenceAltitude`, `heatRate`;
- `partnerBody`, `entryMessage`, `exitMessage`;
- `entryMsgDuration`, `exitMsgDuration`.

El componente se obtiene con `CelestialBody.GetComponent<WormholeComponent>()`.
La lógica real del salto está en el método privado `MakeOrbit(OrbitDriver,
CelestialBody)`. No existe una API `TransformThroughWormhole`.

## APIs verificadas de RemoteTech

- `RTCore.Instance.Satellites`, `.Antennas`, `.Network` y `.Renderer` son
  propiedades públicas.
- `IAntenna` expone `Activated`, `Powered`, `CanTarget`, `Target`, `Dish`,
  `CosAngle`, `Omni` y `Guid`.
- `ISatellite` expone `Guid`, `Position`, `Body`, `Powered`, `CanRelaySignal`,
  `Antennas`, `isVessel` y `parentVessel`.
- `NetworkManager.Graph` es un diccionario público con setter privado:
  `Dictionary<Guid,List<NetworkLink<ISatellite>>>`.
- `NetworkLink<T>` solo contiene `Target`, `Interfaces` y `Port`; no contiene
  distancia ni metadatos extensibles.
- `NetworkManager.UpdateGraph(ISatellite)` es privado.
- `NetworkManager.FindPath(...)`, `NetworkPathfinder.Solve<T>(...)` y los dos
  overloads de `RangeModelExtensions.DistanceTo` son públicos.
- `NetworkRenderer.UpdateNetworkEdges`, `UpdateNetworkCones`, `CheckVisibility`,
  `OnLinkAdd` y `OnLinkRemove` son privados.

Estas firmas se contrastaron también contra las tablas de métodos y campos de la
DLL suministrada mediante `monodis`.

Se buscó `ModuleRTWormholeBridge`, `RemoteTechWormholeBridge` y `RTWB` en todas
las configuraciones y cadenas de los ensamblados suministrados. No se encontró un
conflicto de nombre; el nombre provisional puede conservarse, sujeto a repetir la
comprobación contra una instalación completa.

## Compatibilidad ejecutable

Las DLL exactas, sus referencias y la firma Harmony están verificadas. Los
saltos A→B y B→A dentro del juego confirmaron identidad radial orbital y
conservación de elementos. La cobertura sigue siendo diagnóstica: no se
inyectarán enlaces hasta validar dos endpoints reales.
