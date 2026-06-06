using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum InteractionMode { PressHold, Tap }
    public GameAction action;
    public InteractionMode mode = InteractionMode.PressHold; // default: press & hold

    private Button button;

    void Awake()
    {
        ActionInput.EnsureExists();
        button = GetComponent<Button>();
        if (button != null && mode == InteractionMode.Tap)
        {
            // For tap mode, use onClick only and ignore pointer down/up events
            button.onClick.AddListener(OnButtonClickTap);
        }
    }

    void OnDestroy()
    {
        if (button != null && mode == InteractionMode.Tap)
            button.onClick.RemoveListener(OnButtonClickTap);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mode == InteractionMode.Tap)
            return; // ignore pointer events in tap mode

        ActionInput.SetActionState(action, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mode == InteractionMode.Tap)
            return; // ignore pointer events in tap mode

        ActionInput.SetActionState(action, false);
    }

    private void OnButtonClickTap()
    {
        // Register a press that lasts only this frame (use frame counter in ActionInput)
        ActionInput.SetActionState(action, true);
        StartCoroutine(ReleaseNextFrame());
    }

    private System.Collections.IEnumerator ReleaseNextFrame()
    {
        yield return null;
        ActionInput.SetActionState(action, false);
    }
}
