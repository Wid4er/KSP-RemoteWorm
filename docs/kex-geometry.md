# Geometría real de KEX-Wormholes

## Hecho verificado: condición de salto

`WormholeComponent.Update()` solo actúa sobre la nave activa y cuando
`FlightGlobals.currentMainBody` es el cuerpo del componente.

1. Por debajo de `influenceAltitude` fuerza la salida del mapa, añade temblor de
   cámara y continúa evaluando el salto.
2. El salto es posible cuando la altitud instantánea está estrictamente entre
   `jumpMinAltitude` y `jumpMaxAltitude`.
3. Solo salta al detectar que la altitud vuelve a aumentar respecto al frame
   anterior, es decir, después del periapsis.
4. Usa un `JumpMarker` para impedir un segundo salto inmediato.

En Kcalbeloh, ambos extremos tienen:

- radio del cuerpo: 10.000 m;
- `influenceAltitude`: 35.000 m;
- `jumpMaxAltitude`: 30.000 m;
- `jumpMinAltitude`: 10 m;
- emparejamiento recíproco WH3141A ↔ WH3141B.

Las magnitudes KEX son **altitudes sobre el radio del cuerpo**, no radios desde
el centro. La superficie de seguridad mínima está por tanto a
`body.Radius + influenceAltitude`.

## Hecho verificado: operación de transporte

El método `MakeOrbit` no calcula un punto de boca ni una rotación. Sus operaciones
geométricas relevantes son:

```text
oldBody = driver.referenceBody
driver.orbit.referenceBody = partner
driver.updateFromParameters()
```

Después actualiza el marco físico, Floating Origin y eventos de SOI. No modifica
explícitamente inclinación, LAN, argumento de periapsis, anomalía, época,
semieje mayor o excentricidad. No escala por los radios de los cuerpos y no
invierte ningún vector de manera explícita.

## Transformación radial verificada en juego

KEX conserva los parámetros orbitales y los reevalúa alrededor del cuerpo
compañero. Las pruebas dentro de KSP midieron:

- A→B: error radial orbital `0°`;
- B→A: error radial orbital `8.5377364625159387E-07°`;
- elementos orbitales conservados en ambos sentidos.

Por tanto, la transformación usada por RTWB es:

```text
uSalida = uEntrada
```

donde cada `u` es el vector radial normalizado respecto a su cuerpo. No se aplica
transformación antipodal ni rotación configurable.

Se inspeccionó el `Assembly-CSharp.dll` exacto de la instalación. Su
`OrbitDriver.updateFromParameters(bool)` llama `Orbit.UpdateFromUT`, copia
`Orbit.pos` y `Orbit.vel`, aplica el swizzle de KSP y coloca la nave respecto a
la posición del nuevo cuerpo. La posición mundial inmediata no sirve para esta
comparación porque todavía refleja el cambio de Floating Origin; los vectores
evaluados de `Orbit` sí son estables y coinciden en ambos lados.

La velocidad merece tratamiento separado: si se conservan los elementos
orbitales pero cambia el parámetro gravitatorio del cuerpo, la magnitud derivada
puede cambiar. RTWB solo necesita el radial para el cono, pero esta diferencia
confirma que no debe inventarse una transformación cartesiana completa.

Para renderizar, el swizzle medido antes de los saltos es exactamente:

```text
worldRelative = (orbital.x, orbital.z, orbital.y)
```

La cobertura compara vectores dentro del marco orbital y no necesita esta
conversión; los puntos que se suman a `body.position` sí deben aplicarla.

## Decisión de diseño

MVP 2 y 3 usan identidad sobre coordenadas orbitales relativas normalizadas. La
telemetría de salto se conserva para detectar regresiones si cambia KEX.

## Puntos de entrada y salida para señales

KEX no define una superficie física independiente. Para RTWB se usará una
superficie lógica coherente con el límite seguro:

```text
transitionRadius = body.Radius + influenceAltitude
entryPoint = body.position + uEntrada * transitionRadius
exitPoint = partner.position + uSalida * partnerTransitionRadius
```

Esto es una **decisión de diseño**, no una coordenada usada por KEX. Evita colocar
el punto dentro de la zona incontrolable y comparte una única geometría entre
validación y renderizado.

## Diferencia con WormholeSignalBridge

WSB 2.0.0 crea un nodo proxy en una latitud/longitud fija: el lado del agujero
orientado hacia su cuerpo padre, a una altitud calculada. Esa boca se puede fijar
mediante una mecánica de descubrimiento. Es adecuada para su presupuesto RF de
RealAntennas, pero no representa la transformación de KEX y contradice el modelo
radial requerido por RTWB. Solo se reutilizarán ideas de registro, ciclo de vida y
degradación segura.
