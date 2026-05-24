Level 2 Setup

What I added
- `Assets/Scripts/LevelGenerator.cs` - runtime procedural generator. Use the `Generate()` context menu or enable `autoGenerateInEditor` to create a dungeon in the Editor.
- `Assets/Editor/CreateLevel2Scene.cs` - Editor menu `Tools/Create Level 2 Scene` creates `Assets/Scenes/Level2.unity` and places a configured `LevelGenerator` GameObject.

How to create and test Level 2 in Unity
1. Open Unity Editor and let assets compile.
2. In the menu bar choose `Tools > Create Level 2 Scene`.
   - This will create `Assets/Scenes/Level2.unity`.
3. Open `Assets/Scenes/Level2.unity`.
4. Select the `Level2_Root` GameObject and in the Inspector:
   - Assign `floorPrefab` and `wallPrefab` (optional). If you don't have tile prefabs the generator will create simple quads.
   - Assign `enemyPrefab` and `bossPrefab` (optional) to use your existing enemy prefabs.
   - Assign `playerPrefab` if you want a player spawn to be created.
5. Press the context menu `...` on the component and choose `Generate Level`, or enable `Auto Generate In Editor`.

Notes
- The generator uses simple random rooms and corridors; it's intended to reuse your prefabs.
- If prefabs are not assigned the generator falls back to simple coloured quads so you can see the layout immediately.
- After testing you can tweak `roomCount`, `minRoomSize`, `maxRoomSize`, `width`, and `height` to create varied dungeons.

Next steps you might want me to do
- Place pickable items, health, or loot in rooms.
- Use existing Tilemap / Tiled assets instead of quads.
- Add navmesh or pathfinding bake step / spawn enemy waves.
