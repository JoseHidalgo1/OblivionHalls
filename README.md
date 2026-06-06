# Oblivion Halls

Oblivion Halls es un proyecto de juego 2D desarrollado en Unity con mecánicas de exploración, combate, puzles, control de puertas mediante palancas y sistemas avanzados de olas de enemigos.

## Resumen

Este proyecto usa Unity 2024.3.9f1 y Universal Render Pipeline. El juego está basado en tilemaps y combina:

- Movimiento de jugador con animaciones direccionales.
- Sistema de salud, comida y energía.
- Enemigos con persecución, ataques cuerpo a cuerpo y orientación dinámica.
- Cofres interactivos con loot y drop de items.
- **Puertas y palancas enlazadas** con detección de paso del jugador.
- **Sistema de oleadas de enemigos (WaveSpawner)** con enemigos de una sola vida en combate.
- Control de sorting layer para renderizado correcto.
- **Múltiples escenas:** Menú principal, Nivel 1 (GameScene), Nivel 2 (GameScene 2).
- Sistema de música persistente entre escenas.
- Transiciones de nivel con UI de victoria y derrota.

## Características principales

### Jugador
- Movimiento con WASD / flechas.
- Ataque con la tecla `E`.
- Interacción con la tecla `F`.
- Correr con `Shift`.
- Inventario con `Tab`.
- Sistema de vida, hambre y energía.

### Enemigos
- IA de detección y persecución automática.
- Ataques cuerpo a cuerpo con daño configurable.
- Drop de loot (medicina y pastillas) al morir.
- Orientación dinámica del sprite según dirección de movimiento.
- **Tipos de enemigos:** Centauro, Huargo, Esqueleto, Zombie, Calabaza, Ciclope, Espíritu, Minotauro, Murciélago, Duende, Orco, Slimes.

### Puertas y Palancas
- Palancas interactivas que abren/cierran puertas.
- Detección automática cuando el jugador pasa por puertas abiertas.
- **Trigger de paso independiente** para evitar cierre prematuro.
- La puerta se cierra solo después de que el jugador termina de atravesarla.

### Sistema de Oleadas (WaveSpawner)
- **Tres oleadas de enemigos** que se disparan al cerrar la puerta.
- Los enemigos de oleadas tienen **1 punto de vida** (mueren de un golpe).
- Delay configurable entre oleadas.
- Activación automática de panel `Level Complete` tras completar todas las oleadas.

### Escenas
- **LoadingScene:** Pantalla de carga.
- **MainMenu:** Menú principal con botones de jugar y salir.
- **GameScene:** Nivel 1 con puertas y palancas.
- **GameScene 2:** Nivel 2 con sistema de oleadas de enemigos.

### Sistema de Audio
- Música de exploración persistente por nivel.
- Música de victoria al completar un nivel.
- Cambio de música al entrar a sala de jefe.

### UI
- Pantalla de `Game Over` al morir.
- Pantalla de `Level Complete` al vencer a todos los enemigos.
- Prompts de interacción sobre palancas y puertas.
- HUD en tiempo real con vida, comida y energía.
- Inventario gráfico.

## Requisitos

- Unity 2024.3.9f1
- Paquetes incluidos en `Packages/manifest.json`, entre ellos:
	- `com.unity.render-pipelines.universal` 17.3.0
	- `com.unity.inputsystem` 1.18.0
	- `com.unity.2d.tilemap`
	- `com.unity.2d.animation`
	- `com.unity.ugui`
	- `com.unity.textmeshpro`

## Scripts principales

- **EnemyController.cs** - Control de enemigos, IA, salud y loot.
- **LeverDoor.cs** - Sistema de puertas y palancas con detección de paso.
- **LeverDoorPassTrigger.cs** - Helper para trigger de paso independiente.
- **WaveSpawner.cs** - Sistema de oleadas de enemigos con configuración flexible.
- **BossRoomTrigger.cs** - Dispara oleadas al entrar a sala.
- **PlayerHealth.cs** - Sistema de salud del jugador.
- **PlayerMovement.cs** - Control de movimiento.
- **PlayerAttack.cs** - Sistema de ataque.
- **GameMusicManager.cs** - Persistencia de música entre escenas.
- **LevelCompleteUI.cs** - Panel de victoria con transiciones de nivel.
- **GameOverUI.cs** - Panel de derrota con reinicio.
- **InventoryUI.cs** - Sistema de inventario gráfico.

## Cómo ejecutar el proyecto

1. Clona el repositorio:
	 ```bash
	 git clone https://github.com/JoseHidalgo1/OblivionHalls.git
	 ```
2. Abre la carpeta `Oblivion Halls` en Unity Hub.
3. Deja que Unity restaure los paquetes y compile el proyecto.
4. Abre la escena principal desde `Assets/Scenes/MainMenu.unity`.
5. Ejecuta el juego con `Play`.

## Controles

- Movimiento: `W`, `A`, `S`, `D` o flechas.
- Atacar: `E`.
- Interactuar: `F`.
- Correr / sprint: `Shift`.
- Inventario: `Tabulador`.

## Estructura del proyecto

- `Assets/Scripts/` - Lógica del juego (PlayerController, EnemyController, LeverDoor, WaveSpawner, etc.).
- `Assets/Scenes/` - Escenas de Unity (MainMenu, GameScene, GameScene 2, LoadingScene).
- `Assets/Prefabs/` - Prefabs de enemigos, items, UI y objetos interactivos.
- `Assets/Animations/` - Animaciones de todos los tipos de enemigos.
- `Assets/Enemigo/` - Sprites de enemigos organizados por tipo.
- `Assets/Personaje/` - Sprites y animaciones del jugador.
- `Assets/Items/` - Sprites y prefabs de items.
- `Assets/Banda sonora/` - Música del juego.
- `Assets/Mapa/` - Tilemaps y archivos Tiled.
- `Assets/Settings/` - Configuraciones de URP y recursos.
- `Packages/manifest.json` - Dependencias de Unity.
- `ProjectSettings/` - Configuración del proyecto.

## Mejoras implementadas recientemente

### Última actualización
- **Sistema de Oleadas (WaveSpawner):** Genera 3 oleadas de enemigos progresivas.
- **Puertas inteligentes:** Trigger de paso independiente que detecta cuando el jugador termina de atravesar.
- **Enemigos de oleada con 1 HP:** Los enemigos spawneados por oleadas mueren de un golpe.
- **Segundo nivel completamente funcional:** GameScene 2 con sistema de oleadas integrado.
- **Música persistente:** La música de cada nivel se mantiene al cambiar escenas.
- **Orientación de enemigos mejorada:** Nuevos tipos de enemigos con orientación correcta del sprite.
- **Soporte móvil Android:** HUD táctil móvil con joystick y botones personalizables.
- **HUD móvil oculto en PC:** el HUD de Android ya no aparece cuando se juega en ordenador.
- **Interacción táctil de inventario:** doble toque para usar, pulsación larga para soltar y arrastrar fuera del inventario para descartar.
- **Texto de interacción actualizado:** pista de recogida en suelo cambiada a `Interactuar`.

## Notas de desarrollo

- Compilado en **Unity 2024.3.9f1** con **URP 17.3.0**.
- El sistema de puertas usa **tilemaps separadas** para estado abierto/cerrado.
- Las oleadas de enemigos se instancian dinámicamente con prefab mapping.
- El trigger de paso es un GameObject independiente para evitar conflictos de movimiento.
- Todos los scripts usan **Debug.Log** para facilitar debugging en consola.

## Controles del Juego

- **Movimiento:** `W`, `A`, `S`, `D` o flechas.
- **Atacar:** `E`.
- **Interactuar:** `F` (puertas, palancas, cofres).
- **Correr:** `Shift`.
- **Inventario:** `Tab`.

## Contribuir

Si deseas contribuir, abre un issue o PR con:
- Nuevos enemigos o tipos de enemigos.
- Nuevas mecánicas o niveles.
- Correcciones de bugs.
- Mejoras en rendimiento.

## Licencia

Este proyecto es de código abierto. Consulta la licencia para más detalles.

---

**Última actualización:** Mayo 25, 2026  
**Versión actual:** 2.0 (WaveSpawner + Nivel 2 + Sistema de puertas mejorado)
