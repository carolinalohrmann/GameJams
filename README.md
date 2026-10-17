# GameJams - Tutorial Jungle

Unity 6000.5.5f1. Abrir `Assets/0_0Scenes/Level1.unity` y pulsar Play.

A/D o flechas para moverse, Espacio/W/arriba para saltar, clic izquierdo para disparar,
R para volver al inicio. El jugador es el caballo de la rama
`SpraysYanimaciones`, con reposo, caminar, salto y muerte. El punto de disparo
sigue siendo el hijo `shootp`. El arma especial se dibuja encima de su pistola.

El escenario usa tiles y vegetacion del paquete Jungle incluido en la rama.
Hay plataformas opcionales para practicar el doble salto y un granero como llegada en x=116. Los Skeleton patrullan, se detienen al
tocar al jugador y no se pueden empujar. El tirador conserva el cuadrado
provisional: Flying Eye queda pendiente del asset correcto por indicacion del usuario.

Todos los sprites conservan sus colores originales, sin tintes agregados.
No se usa CharacterPhysics. Library, Logs, Temp y UserSettings estan ignorados.

## Poderes del tutorial

Todos usan el mismo prefab `Assets/0_2prefafs/PowerUpCapsule.prefab`, sin fondo blanco y con su icono
original y un efecto configurable. Se distribuyen por el recorrido en este orden:

| Icono | Efecto |
| --- | --- |
| Apple | +10 de vida maxima y actual, permanente; no se puede acumular repitiendo la misma recogida. |
| Botiquin | Cura 2 corazones sin superar la vida maxima. Con vida completa no cambia nada. |
| Citrus | +50% de movimiento y cadencia de disparo durante 10 segundos. |
| Pluma | Permite un segundo salto en el aire durante 5 segundos. No permite un tercer salto. |
| Resistencia | Inmunidad al dano durante 10 segundos. Las caidas siguen devolviendo al inicio. |
| Camisa | Superpone minigun, sin limite de municion y con disparos cada 0.045 s durante 5 segundos. |
| Lanzapiedra | 5 disparos sin limite de tiempo; 0.9 s entre tiros y 999 de dano, letal para enemigos normales. |
| Chicken | Superpone AK durante 10 segundos, con disparos cada 0.12 s. |

La pistola normal tiene una pausa de 0.35 s. Citrus divide la pausa del arma
actual entre 1.5. Al recoger de nuevo un efecto temporal se renueva su duracion;
no se suman multiplicadores. La ultima arma temporal recogida reemplaza la
anterior. Al terminar una temporal se vuelve a la AK si aun le queda tiempo, o a
la pistola. Recoger Chicken activa o renueva 10 segundos de AK; su tiempo tambien transcurre mientras se usa otra arma.

La vida maxima y la recogida de Apple se guardan con PlayerPrefs.
Se conservan al morir, reiniciar la escena y cerrar/abrir el juego. La AK y Pluma no se guardan; sus guardados permanentes anteriores se ignoran. Cada enemigo
normal tiene 2 de vida. Al morir el jugador reproduce su animacion y reaparece
con la vida completa, conservando sus mejoras. El HUD muestra vida, arma y
solamente los estados activos y avisos contextuales del tutorial.

## Edicion y validacion

`GameJams > Construir tutorial con assets` reconstruye Level1 y sus assets de
animacion desde la base SampleScene. Reemplaza el diseno de Level1: no usarlo
si se quieren conservar cambios manuales de esa escena.

Las animaciones editables estan en `Assets/Animations`; el arte importado,
en `Assets/Art`. Los archivos originales extraidos de la rama se conservan en
las carpetas de la raiz solo como copias locales excluidas de Git; los assets necesarios para jugar estan en Assets. No se descargo arte externo adicional.

Para validar: iniciar Unity en batch mode con `-executeMethod ValidateTutorial.Run`,
sin `-quit`. La prueba usa una ranura de guardado aislada y limpia sus propias
preferencias. Verifica las animaciones, los ocho poderes, los temporizadores,
los limites de disparo/salto, dano, contacto sin empujar, mejoras guardadas,
muerte, regreso al inicio y granero. `ValidateLevelOne.Run` redirige a la misma prueba.

Las armas tienen escala propia y se alinean con la pistola en ambas direcciones. Las piedras lanzadas miden 0.85 unidades de ancho; la piedra sostenida es mas pequena. El granero usa un pivote en la base visible para apoyarse sobre el cesped.

El jugador usa CapsuleCollider2D y deja de empujar contra paredes para evitar quedarse pegado a las esquinas. Las plataformas flotantes usan uno o dos sprites estirados; se elimino la plataforma opcional Descanso. Los efectos temporales tienen 5 segundos por defecto; Citrus, Resistencia y AK conservan sus 10 segundos, y Apple sigue siendo permanente y Pluma dura 5 segundos. El botiquin es instantaneo.

El lanzapiedra no caduca: termina exclusivamente al gastar sus cinco disparos. La escena incluye un Canvas editable con guia ilustrada de los ocho poderes, movimiento, salto/doble salto, disparo y regreso al inicio. Aparece al empezar y se abre/cierra con H; Enter o el boton permiten continuar. La guia pausa el juego y sus temporizadores. El HUD muestra vida, arma y controles durante la partida. Se reemplazo el OnGUI anterior para evitar duplicados.

Los iconos de power ups son un 50% mas grandes, con triggers ajustados y prioridad visual sobre personajes. La vegetacion pequena deja al menos 0.7 unidades libres respecto al recorrido completo de cada enemigo. El bloqueo lateral del jugador tambien contempla enemigos para evitar quedarse pegado al saltar sin eliminar el dano por contacto.
