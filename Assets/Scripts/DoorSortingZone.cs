using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cambia el sorting de los renderers asociados a una puerta cuando el jugador pasa por ella.
/// Este componente es independiente de los scripts de palanca/puerta existentes.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorSortingZone : MonoBehaviour
{
    [Header("Objetos de puerta")]
    [Tooltip("GameObjects que contienen los renderers específicos de esta puerta: Doors, PuertaCerrada, PuertaAbierta.")]
    [SerializeField] private GameObject[] targetObjects = new GameObject[0];

    [Header("Sorting normal")]
    [SerializeField] private string normalSortingLayer = "Background";
    [SerializeField] private int normalSortingOrder = -5;

    [Header("Sorting activo")]
    [SerializeField] private string activeSortingLayer = "Doors";
    [SerializeField] private int activeSortingOrder = 0;

    private readonly List<Renderer> m_TargetRenderers = new List<Renderer>();
    private int m_PlayerCount = 0;

    private void Reset()
    {
        // Asegurar que el trigger está activo en el Collider2D
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void Awake()
    {
        CacheTargetRenderers();
    }

    private void Start()
    {
        ApplySorting(normalSortingLayer, normalSortingOrder);
    }

    private void CacheTargetRenderers()
    {
        m_TargetRenderers.Clear();
        foreach (var go in targetObjects)
        {
            if (go == null)
            {
                continue;
            }

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (!m_TargetRenderers.Contains(renderer))
                {
                    m_TargetRenderers.Add(renderer);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        m_PlayerCount++;
        if (m_PlayerCount == 1)
        {
            ApplySorting(activeSortingLayer, activeSortingOrder);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        m_PlayerCount = Mathf.Max(0, m_PlayerCount - 1);
        if (m_PlayerCount == 0)
        {
            ApplySorting(normalSortingLayer, normalSortingOrder);
        }
    }

    private bool IsPlayer(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player") || collider.gameObject.name == "Jugador" || collider.gameObject.name == "Player")
        {
            return true;
        }

        return false;
    }

    private void ApplySorting(string sortingLayer, int sortingOrder)
    {
        for (int i = 0; i < m_TargetRenderers.Count; i++)
        {
            var renderer = m_TargetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void OnValidate()
    {
        if (targetObjects == null)
        {
            targetObjects = new GameObject[0];
        }

        if (Application.isPlaying)
        {
            CacheTargetRenderers();
            ApplySorting(m_PlayerCount > 0 ? activeSortingLayer : normalSortingLayer, m_PlayerCount > 0 ? activeSortingOrder : normalSortingOrder);
        }
    }
}
