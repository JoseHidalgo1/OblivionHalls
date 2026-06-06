Pasos para preparar y exportar a Android (resumen)

1) Instalar Android Build Support en Unity Hub
   - Abre Unity Hub -> Installs -> Add modules: Android Build Support + SDK & NDK Tools + OpenJDK

2) Configurar SDK/NDK/JDK (si usas externos)
   - En `Edit > Preferences > External Tools` apunta a las rutas instaladas o deja que Unity use el OpenJDK/SDK gestionado.

3) Ejecutar ajuste rápido en Unity
   - En Unity, menú `Build > Configure Android Settings` (script añadido) para aplicar valores recomendados.
   - Revisa `Project Settings > Player > Other Settings`:
     - `Scripting Backend`: IL2CPP
     - `Target Architectures`: ARM64
     - `Target API Level`: usa el más reciente compatible (Google Play requiere nivel alto)

4) Firma y keystore
   - `File > Build Settings > Player Settings > Publishing Settings`: crea o importa tu keystore y guarda la contraseña en un lugar seguro.

5) Input y UI móviles (ya añadidos)
   - Se añadieron scripts:
     - `Assets/Scripts/Input/ActionInput.cs` (gestiona input teclado y móvil)
     - `Assets/Scripts/Input/VirtualJoystick.cs` (joystick táctil)
     - `Assets/Scripts/Input/MobileButton.cs` (botones táctiles)
   - Cómo añadir en escena (Manual):
     - Crear `Canvas` con `Render Mode: Screen Space - Overlay`.
     - Añadir un `Panel` para HUD y crear un `RectTransform` en la esquina inferior izquierda.
     - Añadir `VirtualJoystick` como hijo: ajustar `handle` si quieres; tamaño recomendado 220x220 en pantalla de referencia.
     - Añadir botones (`Button` con `Image`) y añadir el componente `MobileButton`, asignando `action` (Attack, Interact, Pause, ToggleMap, Inventory, Pickup).

6) Optimización
   - Comprimir texturas, reducir resolución máxima, activar stripping y managed code stripping en `Player Settings`.
   - Configurar Quality Settings para móviles.

7) Build
   - `File > Build Settings`: Platform -> Android -> Switch Platform.
   - Build System: Gradle; Build App Bundle (AAB) marcado.
   - Build and Run en dispositivo para probar.

8) Google Play
   - Subir AAB a Google Play Console, crear ficha, añadir imágenes, clasificaciones, contenido.

Notas:
- He actualizado scripts para usar `ActionInput` en lugar de checks directos de `Keyboard.current` en `PlayerMovement`, `MapUIController`, `LeverDoor`, y `ControlsPanel`.
- No puedo instalar Unity o módulos desde aquí; ejecuta los pasos 1-4 en tu máquina y luego prueba el build.
