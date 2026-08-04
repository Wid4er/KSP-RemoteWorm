# Construcción

## Núcleo y plugin disponibles

El núcleo y el plugin compilan con Mono/MSBuild como .NET Framework 4.8:

```bash
KSP_ROOT="/ruta/a/Kerbal Space Program" \
  msbuild RemoteTechWormholeBridge.sln /t:Rebuild /p:Configuration=Release
mono build/inspection/RemoteTechWormholeBridge.Core.Tests.exe
```

Salida:

```text
build/inspection/
  RemoteTechWormholeBridge.Core.dll
  RemoteTechWormholeBridge.Core.Tests.exe
```

El contenido de `build/inspection` no es instalable. El plugin se genera aparte
en `build/GameData`.

## Dependencias del plugin KSP

Se usará una propiedad configurable, nunca una ruta absoluta embebida:

```text
KSP_ROOT=/ruta/a/Kerbal Space Program
KSP_MANAGED_DIR=$KSP_ROOT/KSP_x64_Data/Managed  # Windows
KSP_MANAGED_DIR=$KSP_ROOT/KSP_Data/Managed      # Linux
KSP_GAMEDATA_DIR=$KSP_ROOT/GameData
```

Referencias usadas con copia local desactivada:

- `Assembly-CSharp.dll`;
- `UnityEngine.CoreModule.dll` y módulos realmente usados;
- `RemoteTech/Plugins/RemoteTech.dll`;
- `KopernicusExpansion/Plugins/KEX-Wormholes.dll`;
- `000_Harmony/0Harmony.dll`.

RemoteTech 1.9.12 fue compilado para .NET Framework 4.5. WSB 2.0.0 usa net481.
El plugin y el núcleo usan net48, validado contra la instalación KSP objetivo.

## Verificaciones del build

1. Falla con mensaje claro si falta una referencia.
2. Compila sin copiar DLL de terceros.
3. Ejecuta las pruebas del núcleo.
4. El smoke test verifica que Harmony acepta las firmas Prefix/Postfix/Finalizer.
5. `pedump --verify all` valida el IL con las referencias objetivo disponibles.
6. El paquete contiene únicamente archivos propios del mod.

Salida generada:

```text
build/GameData/RemoteTechWormholeBridge/
  Plugins/RemoteTechWormholeBridge.dll
  Patches/RemoteTechWormholeBridge.cfg
  Localization/
  RemoteTechWormholeBridge.version
```

## Referencias locales verificadas

Se localizó una instalación Steam con build `03190` que contiene las referencias
reales necesarias. El build debe recibir su raíz mediante `KSP_ROOT`; no debe
incrustar la ruta descubierta ni copiar estas DLL al paquete.

Versiones de assembly comprobadas:

- RemoteTech `1.9.0.0`;
- KEX-Wormholes `1.0.0.0`;
- Kopernicus `1.0.247.0`;
- Harmony `2.2.1.0`.

El bloqueo de referencias y la transformación radial quedan resueltos. El
siguiente límite es validar la cobertura dentro de Unity/KSP antes de habilitar
la inyección de enlaces.
