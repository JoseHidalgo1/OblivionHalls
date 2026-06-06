using UnityEngine;

[DefaultExecutionOrder(-300)]
public class AndroidQualityRuntimeEnforcer : MonoBehaviour
{
    void Awake()
    {
        // Enforce high-quality settings at runtime for Android
        if (Application.platform == RuntimePlatform.Android)
        {
            // Set highest quality level available
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);

            // Ensure high render scale
            Time.timeScale = 1f; // Keep time normal

            // Limit frame rate to device max (usually 60Hz on mobile, some 120Hz)
            Application.targetFrameRate = 60;

            Debug.Log($"[AndroidQualityRuntimeEnforcer] Quality level set to: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        }
    }
}
