# GameJams - Tutorial, mundos y rutas finales

Unity 6000.5.5f1. Abrir `Assets/0_0Scenes/Tutorial.unity` y pulsar Play.

Las ocho escenas son Tutorial y Level2 a Level8.
Tutorial conserva el tutorial original fijo: plataformas de practica, los ocho poderes,
guia completa, 14 enemigos y el granero. El generador no reemplaza el tutorial.
Level2 crea un recorrido nuevo con hasta tres poderes distintos y mas enemigos,
combinando cuerpo a cuerpo y Flying Eye en lugar de colocar solo unos pocos tiradores.
Level3 usa las tres capas de fondo del paquete Tiny Platformer - Forest y sus plataformas,
con parallax, enemigos de cuerpo a cuerpo y Flying Eye, y hasta tres poderes por partida.
Level4 usa Multi Platformer con cinco capas de fondo y mas plataformas elevadas.
Level5 usa Tiny Forest con un recorrido mas largo, plataformas mas cortas y saltos mayores.
La progresion es Tutorial -> Level2 -> Level3 -> Level4 -> Level5 -> Level6.
Level6 usa Final.zip: Necromancer de 301 de vida con abanicos de proyectiles,
invocaciones, llamas y un ritual letal que avisa durante 1.6 segundos.
Al derrotarlo aparecen Torre (final normal) y portal (ruta secreta a Level7).
Se elige la entrada acercandose y pulsando E; caminar junto a una puerta no la activa.
Level7 mezcla Final, Jungle, Multi Platformer y Tiny Forest con leves desplazamientos
visuales. Sus enemigos tienen 10 de vida y hacen 2 de dano. Caen meteoritos
con aviso previo de 1.2 segundos; meteoritos y rituales letales matan incluso con
Resistencia o muchos corazones. Al final espera otro Necromancer de 400 de vida,
con los mismos ataques e invocaciones del nivel 6. Ambos invocan esqueletos solo
a su izquierda, sin superponerlos con actores ni plataformas y con un maximo de tres vivos.
Los esqueletos invocados tienen 10 de vida.
Su Torre se activa al acercarse, sin E, y lleva a Level8: arena corta y fija con siete plataformas elevadas y Cazador de 666 de vida,
movimiento rapido y rafagas de efecto 008 cada 0.065 segundos, con pausas para esquivar. Tambien caen meteoritos en esta arena.
Citrus y Pluma aparecen siempre en la arena del Cazador.
Al vencerlo aparece: "Felicidades, eres housestatico".
Las piedras hacen 20 de dano a los jefes; conservan el golpe letal contra enemigos normales.
La vida base del jugador es de 10 corazones. Los guardados anteriores reciben 5 corazones adicionales una sola vez, conservando las mejoras de Apple y las penalizaciones anteriores.
La pantalla inicial del Tutorial incluye Restablecer valores: vuelve a 10 de vida actual/maxima y 1 de dano, y guarda el cambio. El boton desaparece al empezar y no se ofrece en la guia durante la partida ni en otros niveles.
Morir, caer o pulsar R resta 5 de vida maxima, de forma acumulativa y con minimo de 1.
Los botones para volver al tutorial piden confirmacion; Cancelar conserva el nivel actual.
Cada bala del Cazador causa 6 de dano.
La nueva vida maxima se guarda y se mantiene al cambiar de nivel. El jugador reaparece
con esa vida; restaura al jefe sin derrotar y limpia sus ataques, conservando el recorrido.
Los Necromancers se desplazan durante el combate, incluso al lanzar sus ataques.
Solo las dos puertas del nivel 6 requieren E; las salidas de una sola puerta son automaticas.
Todos los proyectiles del jugador se destruyen al segundo; los enemigos conservan sus 3 segundos.
La guia es una lista sencilla: icono junto al nombre y descripcion de cada poder. Se abre con H.
La barra superior y la ayuda incluyen un boton para volver al Tutorial.
Granero y Torre usan orden de dibujo 1 y descansan sobre el suelo.
No hay franja inferior ni avisos de rituales, objetivos o poderes durante la partida.
El Tutorial tiene cielo celeste,
Level2 usa un fondo completo del prado y Level6 reduce sus capas de fondo para despejar el combate.
`GameJams > Crear niveles finales 6, 7 y 8` construye las tres escenas finales sin rehacer Tutorial.
`GameJams > Actualizar monstruos y crear niveles 4 y 5` actualiza el tutorial y amplia la campana.
`GameJams > Crear nivel Tiny Forest` crea o actualiza Level3 y sus conexiones, conservando Tutorial.
`GameJams > Construir tutorial y niveles` regenera todas las escenas y sus assets.

A/D o flechas para moverse, Espacio para saltar, clic izquierdo para disparar,
R para volver al inicio. El jugador es el caballo de la rama
`SpraysYanimaciones`, con reposo, caminar, salto y muerte. El punto de disparo
sigue siendo el hijo `shootp`. El arma especial se dibuja encima de su pistola.

La jungla usa Jungle, el prado GrassLand y el bosque Tiny Platformer - Forest, todos locales.
En Level2, el generador coloca plataformas con saltos cortos y una salida sobre suelo real.
El tutorial alterna Skeleton, Goblin y Mushroom de Monsters Creatures Fantasy;
sus tres tiradores son Flying Eye del mismo paquete. Conserva los 14 lugares de combate,
las plataformas, los ocho poderes y el granero. Los demas niveles incluyen tambien al perro zombi.
No se pueden empujar.

Cada entrada o recarga de Level2 a Level7 crea una semilla nueva.
Cambian anchos, alturas, huecos, plataformas elevadas, reparto de enemigos y tres poderes:
la manzana aparece en un 60% de las entradas, junto a otros dos poderes al azar;
si no aparece, se eligen tres poderes diferentes. En Tutorial la manzana tiene un 10% de probabilidad por entrada;
morir o pulsar R no vuelve a sortearla. La arena del Cazador conserva su manzana fija.
El primer Necromancer siempre deja una manzana delante de las dos salidas al ser derrotado.
En Level7 no aparece Camisa; en Level8 no aparecen Camisa ni Chicken/AK.
Los enemigos comunes tienen 6 de vida en Tutorial y Level2-Level6, y 10 en los niveles secretos.
El primer Necromancer tiene 301 de vida y causa 4 de dano por contacto o ritual y 3 por proyectil.
El segundo Necromancer tiene 400 de vida y causa 9 por contacto o ritual y 8 por proyectil;
sus proyectiles viajan a 6.5 y sus ataques tienen menos preparacion y espera.
Los esqueletos invocados conservan 10 de vida y causan 3 de dano con el primer Necromancer y 8 con el segundo.
El contacto con el Cazador mata al instante, incluso con Resistencia; sus balas siguen causando 6 de dano.
La salida normal muestra: "Tener miedo a lo desconocido es normal. Disfruta tu mediocridad."
Los finales normal y secreto muestran tambien los creditos de Carolina Lohrmann y Kevin Illanes, los agradecimientos a los creadores de assets de itch.io y a Miro Haverinen por tres canciones de Fear & Hunger, y "¡Gracias por jugar!".
La tematica de cada escena se mantiene. Morir o pulsar R vuelve al inicio del recorrido
actual; no genera otro mapa hasta volver a cargar la escena. Tutorial y Level8 mantienen su diseno fijo.

Los sprites conservan sus colores originales; las balas de enemigos comunes son cafes.
No se usa CharacterPhysics. Library, Logs, Temp y UserSettings estan ignorados.

Los sonidos locales estan en `Assets/Resources/GameAudio`. Camisa usa akymachinegun al recogerse;
Chicken usa pollo; lanzapiedra usa su sonido de recogida y otro al lanzar cada piedra.
Los demas poderes usan powerups. Minigun y Cazador comparten disparos cazador;
AK usa ak47disparo. El Cazador usa enemigo como respiracion durante el combate.
Tambien hay pasos, salto, disparo normal, dano/muerte del jugador, golpes, muertes de enemigos, magia y meteoritos.
Los ataques sin sonido propio usan espada; las muertes sin sonido propio usan goblindeadd, excepto los jefes.
Los niveles 1 a 4 usan sonido de fondo; el 5, musica fondo desl nivel 5; el 6 y el 7, la nueva musica necromancer de Descargas.
El nivel 8 repite exclusivamente los primeros 40 segundos de su musica final.
Abrir la ayuda pausa el audio de la partida y reproduce el sonido de menu una vez.
Los botones Volver al tutorial y Guia estan en la esquina inferior derecha; Guia no muestra H.

## Poderes del tutorial

Todos usan el mismo prefab `Assets/0_2prefafs/PowerUpCapsule.prefab`, sin fondo blanco y con su icono
original y un efecto configurable. El tutorial incluye los ocho efectos; el nivel 2 elige hasta tres por partida:

| Icono | Efecto |
| --- | --- |
| Apple | +10 de vida maxima y actual y +1 de dano permanente para pistola, AK y minigun; una vez por recogida. Las piedras conservan su dano letal. |
| Botiquin | Cura 2 corazones sin superar la vida maxima. Con vida completa no cambia nada. |
| Citrus | +50% de movimiento y cadencia hasta salir del nivel, incluso tras morir o pulsar R. |
| Pluma | Permite un segundo salto hasta salir del nivel, incluso tras morir o pulsar R. No permite un tercer salto. |
| Resistencia | Inmunidad al dano durante 10 segundos. Las caidas siguen devolviendo al inicio. |
| Camisa | Superpone minigun, sin limite de municion y con disparos cada 0.045 s durante 10 segundos. |
| Lanzapiedra | 5 disparos sin limite de tiempo; 0.9 s entre tiros y 999 de dano, letal para enemigos normales. |
| Chicken | Superpone AK durante 15 segundos, con disparos cada 0.13 s. |

La pistola normal tiene una pausa de 0.35 s. Citrus divide la pausa del arma
actual entre 1.5. Al recoger de nuevo un efecto temporal se renueva su duracion;
no se suman multiplicadores. La ultima arma temporal recogida reemplaza la
anterior. Al terminar una temporal se vuelve a la AK si aun le queda tiempo, o a
la pistola. Recoger Chicken activa o renueva 15 segundos de AK; su tiempo tambien transcurre mientras se usa otra arma.

La vida maxima y el dano se guardan con PlayerPrefs. Cada manzana se recoge una vez por visita:
morir o pulsar R no la repone; volver a cargar o entrar al nivel si la hace aparecer.
Los registros antiguos de manzanas recogidas no ocultan las de la nueva visita.
La AK, Citrus y Pluma no se guardan entre niveles. Los enemigos normales de los niveles 2 a 6
tienen 6 de vida; en el tutorial tambien tienen 6, y en los niveles secretos tienen 10.
En el nivel secreto siguen teniendo 5, salvo el esqueleto que tiene 6. Al morir el jugador reproduce su animacion y reaparece
con la vida completa, conservando sus mejoras. El HUD muestra vida, arma y
solamente los estados activos. Las instrucciones y habilidades estan en la guia.

## Edicion y validacion

`GameJams > Construir tutorial con assets` reconstruye Tutorial y sus assets de
animacion desde la base SampleScene. Reemplaza el diseno de Tutorial: no usarlo
si se quieren conservar cambios manuales de esa escena.

Las animaciones editables estan en `Assets/Animations`; el arte importado,
en `Assets/Art`. Los archivos originales extraidos de la rama se conservan en
las carpetas de la raiz solo como copias locales excluidas de Git; los assets necesarios para jugar estan en Assets. No se descargo arte externo adicional.

Para validar: iniciar Unity en batch mode con `-executeMethod ValidateGameLevels.Run`,
sin `-quit`. Verifica diez partidas entre las cinco escenas, el tutorial fijo con ocho
poderes y 14 enemigos variados, cambios de geometria al repetir cada nivel procedural,
los fondos Multi/Tiny, saltos con la fisica del jugador, orientacion del perro zombi y transiciones.
`ValidateTutorial.Run` y `ValidateLevelOne.Run` redirigen a esa prueba.
`ValidateTutorialCanvas.Run` verifica la pausa de la guia, Citrus/Pluma hasta salir del nivel y cinco
disparos de piedra sin caducidad. `PreviewGameLevels.Run` genera vistas de las cinco escenas.
`ValidateFinalCampaign.Run` verifica la entrada desde Level5, ambas rutas, vidas de jefes,
colisiones, saltos, invocaciones, meteoritos/rituales letales, reinicios, efecto 008 del
Cazador, repeticion de mapas y el final secreto. `PreviewFinalCampaign.Run` genera vistas
de los tres mundos finales, las puertas, los jefes y el mensaje final.

Las armas tienen escala propia y se alinean con la pistola en ambas direcciones. Las piedras lanzadas miden 0.85 unidades de ancho; la piedra sostenida es mas pequena. El granero usa un pivote en la base visible para apoyarse sobre el cesped.

El jugador usa CapsuleCollider2D y deja de empujar contra paredes para evitar quedarse pegado a las esquinas. Las plataformas flotantes usan uno o dos sprites estirados; se elimino la plataforma opcional Descanso. Los efectos temporales tienen 5 segundos por defecto; Resistencia dura 10 segundos, Camisa 10 y AK 15 segundos. Citrus y Pluma duran hasta salir del nivel; Apple sigue siendo permanente. El botiquin es instantaneo.

El lanzapiedra no caduca: termina exclusivamente al gastar sus cinco disparos. La escena incluye un Canvas editable con una guia de texto de los poderes, movimiento, salto/doble salto, disparo y reinicio con penalizacion. Aparece al empezar y se abre/cierra con H; Enter o el boton permiten continuar. La guia pausa el juego y sus temporizadores. El HUD muestra vida, arma y controles durante la partida. Se reemplazo el OnGUI anterior para evitar duplicados.

Los iconos de power ups son un 50% mas grandes, con triggers ajustados y prioridad visual sobre personajes. La vegetacion pequena deja al menos 0.7 unidades libres respecto al recorrido completo de cada enemigo. El bloqueo lateral del jugador tambien contempla enemigos para evitar quedarse pegado al saltar sin eliminar el dano por contacto.
