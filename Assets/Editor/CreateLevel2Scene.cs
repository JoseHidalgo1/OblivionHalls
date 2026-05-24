#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CreateLevel2Scene
{
    [MenuItem("Tools/Create Level 2 Scene")]
    public static void CreateScene()
    {
        string scenePath = "Assets/Scenes/Level2.unity";

        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create LevelGenerator root
        GameObject root = new GameObject("Level2_Root");
        var gen = root.AddComponent<LevelGenerator>();

        // Set some level-specific defaults
        gen.width = 80;
        gen.height = 60;
        gen.roomCount = 10;
        gen.minRoomSize = 4;
        gen.maxRoomSize = 12;
        gen.tileSize = 1;

        // Try to auto-find some prefabs (best-effort)
        string[] guids = AssetDatabase.FindAssets("t:Prefab Enemigo");
        if (guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:Prefab Enemy");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            gen.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Boss prefab search
        guids = AssetDatabase.FindAssets("t:Prefab Boss");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            gen.bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Player prefab search
        guids = AssetDatabase.FindAssets("t:Prefab Player");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            gen.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(newScene);
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log("Level 2 scene created: " + scenePath + ". Assign floor/wall/player/enemy prefabs in the inspector before testing.");
    }
}
#endif
