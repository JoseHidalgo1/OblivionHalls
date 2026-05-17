# Oblivion Halls

Oblivion Halls es un proyecto de juego 2D desarrollado en Unity con mecánicas de exploración, combate, puzles y control de puertas mediante palancas.

## Resumen

Este proyecto usa Unity 2024.3.9f1 y Universal Render Pipeline. El juego está basado en tilemaps y combina:

- Movimiento de jugador con animaciones direccionales.
- Sistema de salud, comida y energía.
- Enemigos con persecución y ataques cuerpo a cuerpo.
- Cofres interactivos con loot.
- Puertas y palancas enlazadas con interacción y prompts.
- Control de sorting layer para evitar que puertas y jugador se solapen incorrectamente.

## Características principales

- Movimiento con WASD / flechas.
- Ataque con la tecla `E`.
- Interacción con la tecla `F`.
- Puertas automatizadas por palancas.
- Enemigos con IA de detección, ataque y caída de loot.
- Sistema de inventario y objetos.
- HUD de vida, comida y energía.
- Pantallas principal, pantallas de `Game Over` y `Level Complete`.

## Requisitos

- Unity 2024.3.9f1
- Paquetes incluidos en `Packages/manifest.json`, entre ellos:
	- `com.unity.render-pipelines.universal` 17.3.0
	- `com.unity.inputsystem` 1.18.0
	- `com.unity.2d.tilemap`
	- `com.unity.2d.animation`
	- `com.unity.ugui`

## Cómo ejecutar el proyecto

1. Clona el repositorio:
	 ```bash
	 git clone https://github.com/JoseHidalgo1/OblivionHalls.git
	 ```
2. Abre la carpeta `Oblivion Halls` en Unity Hub.
3. Deja que Unity restaure los paquetes y compile el proyecto.
4. Abre la escena principal desde `Assets/Scenes/`.
5. Ejecuta el juego con `Play`.

## Controles

- Movimiento: `W`, `A`, `S`, `D` o flechas.
- Atacar: `E`.
- Interactuar: `F`.
- Correr / sprint: `Shift`.
- Inventario: `Tabulador`.

## Estructura del proyecto

- `Assets/Scripts/` - Lógica del juego.
- `Assets/Scenes/` - Escenas de Unity.
- `Assets/Prefabs/` - Prefabs de objetos, enemigos y objetos interactivos.
- `Assets/Animations/` - Animaciones de personajes y objetos.
- `Assets/Settings/` - Configuraciones de URP y recursos.
- `Packages/manifest.json` - Dependencias de Unity.
- `ProjectSettings/ProjectVersion.txt` - Versión de Unity.

## Notas adicionales

- Si Unity pregunta por el backend de Input System, acepta cambiar al `Input System Package`.
- La mayor parte de los sistemas usan `Tilemap`, `Collider2D` y `TextMesh` para prompts de interacción.
- El control de sorting layer es importante para que las puertas y el jugador se rendericen correctamente en escenas con tilemaps.

## Contribuir

Si deseas contribuir, abre un issue o PR con mejoras en las mecánicas, nuevas escenas o correcciones de bugs.
