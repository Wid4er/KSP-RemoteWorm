# Changelog

## 0.7.0 - 2026-09-11

- Añade una integración opcional con Contract Configurator para contratos de
  carrera únicos por cada par físico de agujeros KEX.
- Expone una API RTWB de solo lectura para catálogo, endpoints y enlaces
  operativos sin duplicar las reglas de validez existentes.
- Calcula sistemas y tiers mediante la jerarquía multistar de Kopernicus y un
  BFS topológico desde el sistema de Kerbin.
- Exige cinco días Kerbin consecutivos de servicio desde un endpoint B real del
  enlace hasta KSC, con continuidad entre gateways redundantes.
- Conserva la no repetición mediante `CONTRACT_ALL` de Contract Configurator y
  mantiene el DLL principal libre de referencias a esa dependencia opcional.
- Permite que una misión aparezca al haber alcanzado el cuerpo padre de cualquiera
  de las dos bocas, sin invertir la orientación topológica A → B del contrato.
- Sustituye la flecha no soportada por la fuente de KSP en el título y amplía el
  briefing con una guía breve para construir y alinear el enlace RTWB.
- Muestra los anillos rojos de la banda operativa al enfocar cualquiera de las
  bocas del par en el mapa, no sólo al seleccionar directamente su relé.
- Hace que las líneas magenta del enlace respondan también a los filtros Path y
  MultiPath, sin exigir que el filtro Dish permanezca activo.

## 0.6.0 - 2026-08-11

- Traslada la correccion general de precision de posiciones a la nueva
  dependencia RemoteTech Overhaul, evitando mantener el mismo parche dentro de
  este plugin.
- Calcula una banda operacional independiente para cada agujero a partir de su
  superficie de transición y su SOI, reservando un 20 % de margen exterior.
- Comprime proporcionalmente la banda en SOI pequeñas sin modificar datos de
  otros mods.
- Limita cada cono al máximo local de su agujero de salida.
- Recorta las aristas de cada cono entre los anillos interior y exterior para
  representar únicamente la banda donde puede colocarse un relé.
- Añade dos anillos rojos que muestran los límites válidos en el plano orbital
  de la nave seleccionada.

## 0.5.0-beta.1 - 2026-08-05

- Integra enlaces bidireccionales de agujero en el grafo de RemoteTech.
- Calcula cobertura geométrica entre relés situados a 100-300 km de la
  superficie de transición.
- Dibuja segmentos locales y conos potenciales magenta sin cuerda interestelar.
- Funciona en vuelo y Tracking Station, incluidas las redefiniciones de objetivo.
- Reconstruye enlaces ante pérdida de energía, docking, undocking y destrucción.
- Corrige la prioridad diagnóstica de antenas inactivas o sin energía.
- Se distribuye bajo GNU GPL v3.0 only sin DLL de terceros.
