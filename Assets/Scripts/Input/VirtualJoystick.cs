using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 50f;

    private RectTransform rect;
    private Image bgImage;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        bgImage = GetComponent<Image>();

        // Ensure Image can receive pointer events
        if (bgImage == null)
        {
            bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.3f);
        }
        bgImage.raycastTarget = true;

        if (handle == null)
        {
            // Create a simple handle if none assigned
            GameObject h = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            h.transform.SetParent(transform, false);
            handle = h.GetComponent<RectTransform>();
            handle.sizeDelta = new Vector2(80, 80);
            Image handleImg = h.GetComponent<Image>();
            handleImg.color = new Color(1f, 1f, 1f, 0.8f);
        }
        ActionInput.EnsureExists();

        // Auto-adjust handleRange if default is small relative to control size
        if (rect != null)
        {
            float suggested = Mathf.Min(rect.rect.width, rect.rect.height) * 0.5f;
            if (handleRange < 10f)
                handleRange = suggested;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(local, handleRange);
        handle.anchoredPosition = clamped;

        Vector2 normalized = clamped / handleRange;
        ActionInput.SetMovement(normalized);
        Debug.Log($"[VirtualJoystick] Drag: local={local}, normalized={normalized}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        ActionInput.SetMovement(Vector2.zero);
        Debug.Log("[VirtualJoystick] PointerUp: movement reset");
    }
}
