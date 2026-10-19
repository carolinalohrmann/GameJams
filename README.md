# GameJams

Proyecto para Unity 6000.5.5f1.

## Abrir el juego

1. Añadir esta carpeta a Unity Hub.
2. Abrir `Assets/0_0Scenes/Tutorial.unity`.
3. Pulsar Play y después Empezar.

Unity genera Library al abrir el proyecto por primera vez.

## Controles

- A/D o flechas: mover.
- Espacio: saltar.
- Clic izquierdo: disparar.
- H: abrir o cerrar la guía.
- R: volver al inicio del nivel.
- E: entrar por una de las dos puertas del nivel 6.

## Niveles

Tutorial y Level8 tienen plataformas fijas. Level2 a Level7 generan un recorrido
nuevo al cargar la escena. Morir o pulsar R conserva el recorrido actual.

El primer Necromancer está en Level6. Tras vencerlo se puede elegir el final
normal o entrar a Level7. La ruta secreta termina en Level8, con el Cazador.

## Jugador y poderes

La vida base es 10 y el daño base es 1. El botón Restablecer valores de la pantalla
inicial del tutorial permite volver a esos valores. La vida máxima y el daño se
guardan entre partidas. Morir, caer o pulsar R resta 5 de vida máxima, con mínimo 1.
Recibir daño y reaparecer no dan invulnerabilidad automática. Resistencia sigue
protegiendo del daño normal mientras dura su efecto.

- Apple: +10 de vida máxima y +1 de daño permanentes.
  Tiene un 10% de probabilidad por entrada en todos los niveles. La recompensa
  del primer Necromancer aparece siempre.
- Botiquín: cura 2 corazones.
- Citrus: +50% de movimiento y cadencia hasta salir del nivel.
- Pluma: doble salto hasta salir del nivel.
- Resistencia: inmunidad durante 10 segundos.
- Camisa: minigun durante 10 segundos.
- Lanzapiedra: 5 disparos.
- Chicken: AK durante 15 segundos.

## Archivos

Los scripts están en `Assets/0_1scripss`, separados por jugador, enemigos, jefes,
niveles, poderes, interfaz y audio. Las escenas, animaciones, prefabs y recursos
están dentro de Assets. La generación de niveles se ejecuta durante la partida.

Para compartir el proyecto se necesitan Assets, Packages y ProjectSettings.
No hace falta incluir Library, Logs, Temp, UserSettings ni las carpetas originales
de recursos que estén fuera de Assets.
