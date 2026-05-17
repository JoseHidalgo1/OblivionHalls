using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Script de debug para verificar si los cofres y trampas están configurados correctamente.
/// Adjunta este script a un GameObject vacío en la escena.
/// </summary>
public class DebugTiledSetup : MonoBehaviour
{
    void Start()
    {
        Debug.Log("========== DEBUG TILED SETUP ==========");

        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Jugador");
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (playerObj != null)
        {
            Debug.Log($"[DEBUG] Jugador encontrado: {playerObj.name}");
            Collider2D playerCollider = playerObj.GetComponent<Collider2D>();
            Debug.Log($"[DEBUG] Jugador tiene Collider2D: {(playerCollider != null ? "SÍ" : "NO")}");
            Debug.Log($"[DEBUG] Tag del jugador: {playerObj.tag}");
        }
        else
        {
            Debug.LogError("[DEBUG] Jugador NO encontrado.");
        }

        // Buscar cofres
        ChestInteraction[] chests = FindObjectsByType<ChestInteraction>(FindObjectsSortMode.None);
        Debug.Log($"[DEBUG] Cofres encontrados: {chests.Length}");
        foreach (ChestInteraction chest in chests)
        {
            Debug.Log($"[DEBUG] Cofre: {chest.gameObject.name} en posición {chest.transform.position}");
        }

        // Buscar Tilemaps
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        Debug.Log($"[DEBUG] Tilemaps encontrados: {tilemaps.Length}");
        foreach (Tilemap tilemap in tilemaps)
        {
            Debug.Log($"[DEBUG] Tilemap: {tilemap.gameObject.name}");
            TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
            Debug.Log($"  - TilemapCollider2D: {(collider != null ? "SÍ" : "NO")}");
            if (collider != null)
            {
                Debug.Log($"  - isTrigger: {collider.isTrigger}");
            }
            Rigidbody2D rigidbody = tilemap.GetComponent<Rigidbody2D>();
            Debug.Log($"  - Rigidbody2D: {(rigidbody != null ? "SÍ" : "NO")}");
            if (rigidbody != null)
            {
                Debug.Log($"  - bodyType: {rigidbody.bodyType}");
                Debug.Log($"  - gravityScale: {rigidbody.gravityScale}");
            }
            TrapCollider trapCollider = tilemap.GetComponent<TrapCollider>();
            Debug.Log($"  - TrapCollider: {(trapCollider != null ? "SÍ" : "NO")}");
        }

        Debug.Log("========== FIN DEBUG ==========");
    }
}
