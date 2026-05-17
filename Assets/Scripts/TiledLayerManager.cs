using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Configura el tilemap de trampas para que no se caiga del mapa.
/// Adjunta este script al GameObject que contiene la capa "traps" del mapa Tiled.
/// </summary>
public class TiledLayerManager : MonoBehaviour
{
    void Start()
    {
        Tilemap trapLayer = GetComponent<Tilemap>();
        if (trapLayer == null)
        {
            Debug.LogError("[TiledLayerManager] No hay Tilemap en este GameObject.");
            return;
        }

        // Configurar Rigidbody2D
        Rigidbody2D trapRigidbody = GetComponent<Rigidbody2D>();
        if (trapRigidbody == null)
        {
            trapRigidbody = gameObject.AddComponent<Rigidbody2D>();
        }

        trapRigidbody.bodyType = RigidbodyType2D.Kinematic;
        trapRigidbody.gravityScale = 0f;
        trapRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;

        // Configurar TilemapCollider2D
        TilemapCollider2D trapCollider = GetComponent<TilemapCollider2D>();
        if (trapCollider == null)
        {
            trapCollider = gameObject.AddComponent<TilemapCollider2D>();
            Debug.Log("[TiledLayerManager] TilemapCollider2D agregado.");
        }
        
        trapCollider.isTrigger = true;

        // Agregar TrapCollider si no existe
        TrapCollider trapDamage = GetComponent<TrapCollider>();
        if (trapDamage == null)
        {
            gameObject.AddComponent<TrapCollider>();
        }

        Debug.Log("[TiledLayerManager] Capa de trampas configurada correctamente.");
    }
}
