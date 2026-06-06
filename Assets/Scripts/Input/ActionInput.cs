using System.Collections.Generic;
using UnityEngine;

public class ActionInput : MonoBehaviour
{
    private static ActionInput instance;

    private HashSet<GameAction> currentPressed = new HashSet<GameAction>();
    private HashSet<GameAction> lastPressed = new HashSet<GameAction>();
    private System.Collections.Generic.Dictionary<GameAction,int> lastPressFrame = new System.Collections.Generic.Dictionary<GameAction,int>();
    private System.Collections.Generic.Dictionary<GameAction,int> lastReleaseFrame = new System.Collections.Generic.Dictionary<GameAction,int>();

    private Vector2 movement = Vector2.zero;
    private Vector2 mobileMovement = Vector2.zero; // Separate mobile input from keyboard

    public static void EnsureExists()
    {
        if (instance != null) return;
        var go = new GameObject("_ActionInput");
        instance = go.AddComponent<ActionInput>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Update lastPressed snapshot for legacy checks
        lastPressed.Clear();
        foreach (var a in currentPressed) lastPressed.Add(a);

        // If a keyboard is available, sample keys via KeyBindings
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            currentPressed.Clear();
            // Movement handled separately below
            foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
            {
                // Skip movement axis (we'll compute using keys)
                if (action == GameAction.MoveUp || action == GameAction.MoveDown || action == GameAction.MoveLeft || action == GameAction.MoveRight)
                    continue;

                var key = KeyBindings.GetKey(action);
                var control = UnityEngine.InputSystem.Keyboard.current[key];
                if (control != null && control.isPressed)
                {
                    currentPressed.Add(action);
                }
            }

            // Movement axis from keys
            float h = (UnityEngine.InputSystem.Keyboard.current[KeyBindings.GetKey(GameAction.MoveRight)]?.isPressed == true ? 1f : 0f)
                      - (UnityEngine.InputSystem.Keyboard.current[KeyBindings.GetKey(GameAction.MoveLeft)]?.isPressed == true ? 1f : 0f);
            float v = (UnityEngine.InputSystem.Keyboard.current[KeyBindings.GetKey(GameAction.MoveUp)]?.isPressed == true ? 1f : 0f)
                      - (UnityEngine.InputSystem.Keyboard.current[KeyBindings.GetKey(GameAction.MoveDown)]?.isPressed == true ? 1f : 0f);

            if (mobileMovement.sqrMagnitude < 0.0001f)
            {
                movement = new Vector2(h, v).normalized;
            }
        }
        else
        {
            // No keyboard: keep currentPressed as set by mobile controls
            // movement is expected to be set by VirtualJoystick
        }
    }

    void LateUpdate()
    {
        // Clear per-frame presses for actions that are no longer pressed
        // (handled by Update swapping sets)
    }

    // Mobile controls call these
    public static void SetActionState(GameAction action, bool pressed)
    {
        EnsureExists();
        if (pressed)
        {
            instance.currentPressed.Add(action);
            instance.lastPressFrame[action] = Time.frameCount;
        }
        else
        {
            instance.currentPressed.Remove(action);
            instance.lastReleaseFrame[action] = Time.frameCount;
        }
    }

    public static void SetMovement(Vector2 value)
    {
        EnsureExists();
        instance.mobileMovement = value.magnitude > 1f ? value.normalized : value;
        instance.movement = instance.mobileMovement;
    }

    public static bool GetAction(GameAction action)
    {
        EnsureExists();
        return instance.currentPressed.Contains(action);
    }

    public static bool WasPressedThisFrame(GameAction action)
    {
        EnsureExists();
        if (instance.lastPressFrame.TryGetValue(action, out int f) && f == Time.frameCount)
            return true;
        return instance.currentPressed.Contains(action) && !instance.lastPressed.Contains(action);
    }

    public static bool WasReleasedThisFrame(GameAction action)
    {
        EnsureExists();
        return !instance.currentPressed.Contains(action) && instance.lastPressed.Contains(action);
    }

    public static Vector2 GetMovement()
    {
        EnsureExists();
        return instance.movement;
    }
}
