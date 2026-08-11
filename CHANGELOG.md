# Changelog

## Unreleased

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
