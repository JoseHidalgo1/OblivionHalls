using System.Collections.Generic;
using UnityEngine;

public class OcclusionTrigger2D : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private string playerTag = "Player";

    [Header("Renderers a superponer")]
    [SerializeField] private Renderer[] occluderRenderers;

    [Header("Orden cuando el jugador entra")]
    [SerializeField] private string occlusionSortingLayer = "Player";
    [SerializeField] private int occlusionSortingOrder = 10;

    private readonly Dictionary<Renderer, (string layer, int order)> originalSorting =
        new Dictionary<Renderer, (string layer, int order)>();

    private void Awake()
    {
        CacheOriginalSorting();
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        ApplyOcclusionSorting();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        RestoreOriginalSorting();
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
        {
            return true;
        }

        return false;
    }

    private void CacheOriginalSorting()
    {
        originalSorting.Clear();

        if (occluderRenderers == null)
        {
            return;
        }

        for (int index = 0; index < occluderRenderers.Length; index++)
        {
            Renderer rendererRef = occluderRenderers[index];
            if (rendererRef == null)
            {
                continue;
            }

            if (!originalSorting.ContainsKey(rendererRef))
            {
                originalSorting.Add(rendererRef, (rendererRef.sortingLayerName, rendererRef.sortingOrder));
            }
        }
    }

    private void ApplyOcclusionSorting()
    {
        if (occluderRenderers == null)
        {
            return;
        }

        for (int index = 0; index < occluderRenderers.Length; index++)
        {
            Renderer rendererRef = occluderRenderers[index];
            if (rendererRef == null)
            {
                continue;
            }

            rendererRef.sortingLayerName = occlusionSortingLayer;
            rendererRef.sortingOrder = occlusionSortingOrder;
        }
    }

    private void RestoreOriginalSorting()
    {
        foreach (KeyValuePair<Renderer, (string layer, int order)> pair in originalSorting)
        {
            Renderer rendererRef = pair.Key;
            if (rendererRef == null)
            {
                continue;
            }

            rendererRef.sortingLayerName = pair.Value.layer;
            rendererRef.sortingOrder = pair.Value.order;
        }
    }

    private void EnsureTriggerCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            return;
        }

        col.isTrigger = true;
    }
}
