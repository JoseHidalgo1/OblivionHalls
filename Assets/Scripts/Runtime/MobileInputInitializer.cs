using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class MobileInputInitializer : MonoBehaviour
{
    void Awake()
    {
        // Ensure ActionInput exists
        ActionInput.EnsureExists();

        // Ensure EventSystem exists for UI input
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        // Ensure touch settings for auto-rotation handled elsewhere
    }
}
