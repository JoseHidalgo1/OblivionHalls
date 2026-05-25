using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;

/// <summary>
/// Maneja el mecanismo de puertas activadas por palancas.
/// Detecta cuando la palanca está activada y abre/cierra la puerta correspondiente.
/// </summary>
public class LeverDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool isLever = false; // Si es true, es una palanca; si es false, es una puerta
    [SerializeField] private LeverDoor linkedLever; // Referencia a la palanca que controla esta puerta
    [SerializeField] private LeverDoor linkedDoor; // Referencia a la puerta controlada por esta palanca

    [Header("Capas Tiled")]
    [SerializeField] private Tilemap closedLeverLayer;
    [SerializeField] private Tilemap openLeverLayer;
    [SerializeField] private Tilemap closedDoorLayer;
    [SerializeField] private Tilemap openDoorLayer;

    [Header("Puerta - Animación")]
    [SerializeField] private float doorOpenDuration = 0.5f;
    [SerializeField] private float doorMoveDistance = 1.0f; // Distancia que se mueve la puerta

    [Header("Puerta - Colisión")]
    [SerializeField] private bool disableColliderWhenOpen = true;
    [SerializeField] private Collider2D doorColliderOverride; // Si la colisión está en otro GameObject, ponerla aquí
    
    [Header("Puerta - Sorting (opcional)")]
    [SerializeField] private string doorSortingLayerName = "Doors";
    [SerializeField] private int doorSortingOrder = 0;
    [Header("Palanca - Sorting (opcional)")]
    [SerializeField] private string leverSortingLayerName = "Doors";
    [SerializeField] private int leverSortingOrder = 0;

    [Header("Sistema de Oleadas")]
    [SerializeField] private WaveSpawner waveSpawner; // Referencia al WaveSpawner
    [SerializeField] private bool triggerWavesWhenDoorCloses = true;
    [SerializeField] private bool triggerWavesOnPlayerPass = true; // Si true, se activan oleadas cuando jugador pasa por puerta abierta
    [SerializeField] private Collider2D playerPassTrigger; // Trigger opcional para detectar paso del jugador

    [Header("Interacción")]
    [SerializeField] private Key interactionKey = Key.F;
    [SerializeField] private string closedPromptMessage = "Presiona F para abrir";
    [SerializeField] private string openedPromptMessage = "Presiona F para cerrar";
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float promptScale = 0.25f;
    [SerializeField] private string promptSortingLayer = "UI";
    [SerializeField] private int promptSortingOrder = 10000;
    [SerializeField] private Color promptColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private float promptFadeDuration = 1.0f;
    [SerializeField] private Font promptFont;

    private bool isActivated = false;
    private Collider2D doorCollider;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isDoorOpen = false;
    private bool isPlayerInRange = false;
    private GameObject promptObject;
    private TextMesh promptText;
    private bool playerHasPassedThroughDoor = false; // Rastrear si el jugador pasó por la puerta abierta
    private BoxCollider2D passTriggerCollider;

    void Start()
    {
        closedPosition = transform.position;

        if (!isLever)
        {
            // Es una puerta
            doorCollider = GetComponent<Collider2D>();
            if (doorCollider == null)
            {
                doorCollider = GetComponentInChildren<Collider2D>();
            }

            passTriggerCollider = CreatePlayerPassTrigger();

            // Calcular posición abierta basada en dirección
            openPosition = closedPosition + Vector3.up * doorMoveDistance;
            UpdateDoorVisuals(isDoorOpen);
        }

        if (isLever)
        {
            UpdateLeverVisuals(isActivated);
        }
    }

    private BoxCollider2D CreatePlayerPassTrigger()
    {
        Collider2D sourceCollider = playerPassTrigger != null ? playerPassTrigger : (doorColliderOverride != null ? doorColliderOverride : doorCollider);
        if (sourceCollider == null)
        {
            Debug.LogWarning("[LeverDoor] No se encontró collider para crear el trigger de paso de jugador. Asigna playerPassTrigger manualmente o añade un collider a la puerta.");
            return null;
        }

        // Buscar un trigger existente en el mismo GameObject (distinto al collider de puerta)
        BoxCollider2D passCollider = null;
        BoxCollider2D[] boxColliders = GetComponents<BoxCollider2D>();
        foreach (var bc in boxColliders)
        {
            if (bc == doorCollider)
                continue;
            if (bc.isTrigger)
            {
                passCollider = bc;
                break;
            }
        }
        if (passCollider == null)
        {
            passCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        Bounds bounds = sourceCollider.bounds;
        passCollider.isTrigger = true;
        passCollider.offset = transform.InverseTransformPoint(bounds.center);
        passCollider.size = new Vector2(bounds.size.x, bounds.size.y);
        passCollider.usedByEffector = false;
        passCollider.enabled = true;

        Debug.Log("[LeverDoor] Trigger de paso creado/actualizado para la puerta abierta.");
        return passCollider;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Primero, permitir que las palancas sigan funcionando
        if (isLever)
        {
            if (!collision.CompareTag("Player") && collision.gameObject.name != "Jugador" && collision.gameObject.name != "Player")
            {
                return;
            }

            isPlayerInRange = true;
            ShowPrompt();
            return;
        }

        // Para puertas abiertas, detectar si el jugador las atraviesa
        if (!isLever && isDoorOpen && triggerWavesOnPlayerPass)
        {
            if (!collision.CompareTag("Player") && collision.gameObject.name != "Jugador" && collision.gameObject.name != "Player")
            {
                return;
            }

            playerHasPassedThroughDoor = true;
            Debug.Log("[LeverDoor] Jugador pasó por la puerta abierta. Cerrando puerta y preparando oleadas...");
            if (linkedLever != null)
            {
                linkedLever.DeactivateLever();
            }
            else
            {
                CloseDoor();
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!isLever)
        {
            return;
        }

        if (!collision.CompareTag("Player") && collision.gameObject.name != "Jugador" && collision.gameObject.name != "Player")
        {
            return;
        }

        // Mantener el prompt visible mientras el jugador siga en rango
        if (!isPlayerInRange)
        {
            isPlayerInRange = true;
            ShowPrompt();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!isLever)
        {
            return;
        }

        if (!collision.CompareTag("Player") && collision.gameObject.name != "Jugador" && collision.gameObject.name != "Player")
        {
            return;
        }

        isPlayerInRange = false;
        HidePrompt();
    }

    void Update()
    {
        if (!isLever || !isPlayerInRange)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[KeyBindings.GetKey(GameAction.Interact)]?.wasPressedThisFrame == true)
        {
            ToggleLever();
        }
        else if (Keyboard.current == null && Input.GetKeyDown(KeyCode.F))
        {
            ToggleLever();
        }
    }

    public void ActivateLever()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;

        UpdatePromptText();
        UpdateLeverVisuals(isActivated);

        // Si es una palanca, activar la puerta vinculada
        if (isLever && linkedDoor != null)
        {
            linkedDoor.OpenDoor();
        }
    }

    public void DeactivateLever()
    {
        if (!isActivated)
        {
            return;
        }

        isActivated = false;

        UpdatePromptText();
        UpdateLeverVisuals(isActivated);

        // Si es una palanca, cerrar la puerta vinculada
        if (isLever && linkedDoor != null)
        {
            linkedDoor.CloseDoor();
        }
    }

    public void ToggleLever()
    {
        if (isActivated)
        {
            DeactivateLever();
        }
        else
        {
            ActivateLever();
        }
    }

    public void OpenDoor()
    {
        if (isDoorOpen || isLever)
        {
            return;
        }

        Debug.Log("[LeverDoor] Abriendo puerta...");
        UpdateDoorVisuals(true);
        DisableDoorColliders(true);
        StartCoroutine(AnimateDoorOpening());
    }

    public void CloseDoor()
    {
        if (!isDoorOpen || isLever)
        {
            return;
        }

        Debug.Log("[LeverDoor] Cerrando puerta...");
        UpdateDoorVisuals(false);
        DisableDoorColliders(false);
        StartCoroutine(AnimateDoorClosing());
    }

    private IEnumerator AnimateDoorOpening()
    {
        float elapsedTime = 0f;

        while (elapsedTime < doorOpenDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / doorOpenDuration);

            // Interpolar posición de cerrada a abierta
            transform.position = Vector3.Lerp(closedPosition, openPosition, t);
        }

        transform.position = openPosition;
        isDoorOpen = true;

        // Desactivar colisión si está configurado
        if (disableColliderWhenOpen)
        {
            if (doorColliderOverride != null)
            {
                doorColliderOverride.enabled = false;
            }
            else if (doorCollider != null)
            {
                doorCollider.enabled = false;
            }
            else
            {
                // Intentar desactivar colliders en los Tilemap layers referenciados
                DisableDoorColliders(true);
            }
        }

        Debug.Log("[LeverDoor] Puerta abierta.");
    }

    private IEnumerator AnimateDoorClosing()
    {
        float elapsedTime = 0f;

        // Reactivar colisión antes de cerrar
        if (disableColliderWhenOpen && doorCollider != null)
        {
            doorCollider.enabled = true;
        }

        while (elapsedTime < doorOpenDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / doorOpenDuration);

            // Interpolar posición de abierta a cerrada
            transform.position = Vector3.Lerp(openPosition, closedPosition, t);
        }

        transform.position = closedPosition;
        isDoorOpen = false;

        // Activar oleadas si el jugador pasó por la puerta
        if (triggerWavesWhenDoorCloses && triggerWavesOnPlayerPass && playerHasPassedThroughDoor && waveSpawner != null)
        {
            Debug.Log("[LeverDoor] Puerta cerrada y jugador pasó. Iniciando oleadas...");
            waveSpawner.StartWaves();
            playerHasPassedThroughDoor = false; // Reset el flag
        }

        Debug.Log("[LeverDoor] Puerta cerrada.");
    }

    // Desactiva/activa colliders que puedan estar en los GameObjects de Tilemap asignados
    private void DisableDoorColliders(bool disable)
    {
        // Primero el override si viene asignado
        if (doorColliderOverride != null)
        {
            doorColliderOverride.enabled = !disable ? true : false;
        }

        // Intentar encontrar TilemapCollider2D o Collider2D en las capas referenciadas
        if (closedDoorLayer != null)
        {
            var c = closedDoorLayer.GetComponent<Collider2D>();
            if (c != null) c.enabled = !disable ? true : false;
        }
        if (openDoorLayer != null)
        {
            var c2 = openDoorLayer.GetComponent<Collider2D>();
            if (c2 != null) c2.enabled = !disable ? true : false;
        }

        // También buscar en hijos por si el collider está en child objects
        if (closedDoorLayer != null)
        {
            foreach (var col in closedDoorLayer.GetComponentsInChildren<Collider2D>(true))
            {
                col.enabled = !disable ? true : false;
            }
        }
        if (openDoorLayer != null)
        {
            foreach (var col in openDoorLayer.GetComponentsInChildren<Collider2D>(true))
            {
                col.enabled = !disable ? true : false;
            }
        }
    }

    private void UpdateLeverVisuals(bool open)
    {
        if (closedLeverLayer != null)
        {
            closedLeverLayer.gameObject.SetActive(!open);
            var ptr = closedLeverLayer.GetComponent<TilemapRenderer>();
            if (ptr != null)
            {
                ptr.sortingLayerName = leverSortingLayerName;
                ptr.sortingOrder = leverSortingOrder;
            }
        }
        if (openLeverLayer != null)
        {
            openLeverLayer.gameObject.SetActive(open);
            var ptr2 = openLeverLayer.GetComponent<TilemapRenderer>();
            if (ptr2 != null)
            {
                ptr2.sortingLayerName = leverSortingLayerName;
                ptr2.sortingOrder = leverSortingOrder;
            }
        }
    }

    private void UpdateDoorVisuals(bool open)
    {
        if (closedDoorLayer != null)
        {
            closedDoorLayer.gameObject.SetActive(!open);
            var tr = closedDoorLayer.GetComponent<TilemapRenderer>();
            if (tr != null)
            {
                tr.sortingLayerName = doorSortingLayerName;
                tr.sortingOrder = doorSortingOrder;
            }
        }
        if (openDoorLayer != null)
        {
            openDoorLayer.gameObject.SetActive(open);
            var tr2 = openDoorLayer.GetComponent<TilemapRenderer>();
            if (tr2 != null)
            {
                tr2.sortingLayerName = doorSortingLayerName;
                tr2.sortingOrder = doorSortingOrder;
            }
        }
    }

    private void ShowPrompt()
    {
        if (promptObject != null)
        {
            return;
        }

        promptObject = new GameObject("LeverPrompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = promptLocalOffset;
        promptObject.transform.localRotation = Quaternion.identity;
        promptObject.transform.localScale = Vector3.one * promptScale;

        promptText = promptObject.AddComponent<TextMesh>();
        promptText.text = isActivated ? openedPromptMessage : closedPromptMessage;
        promptText.fontSize = 10;
        promptText.alignment = TextAlignment.Center;
        promptText.anchor = TextAnchor.MiddleCenter;
        promptText.characterSize = 0.25f;
        promptText.color = promptColor;

        if (promptFont == null)
        {
            promptFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (promptFont != null)
        {
            promptText.font = promptFont;
            MeshRenderer promptRenderer = promptObject.GetComponent<MeshRenderer>();
            promptRenderer.sharedMaterial = promptFont.material;
        }

        MeshRenderer renderer = promptObject.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = promptSortingLayer;
        renderer.sortingOrder = promptSortingOrder;
    }

    private void HidePrompt()
    {
        if (promptObject != null)
        {
            Destroy(promptObject);
            promptObject = null;
        }
    }

    private void UpdatePromptText()
    {
        if (promptText != null)
        {
            promptText.text = isActivated ? openedPromptMessage : closedPromptMessage;
        }
    }

    // Para debugging: visualizar el tipo de objeto
    void OnDrawGizmos()
    {
        if (isLever)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * doorMoveDistance);
        }
    }
}
