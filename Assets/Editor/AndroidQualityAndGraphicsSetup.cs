using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AndroidQualityAndGraphicsSetup
{
    [MenuItem("Build/Configure Android Quality & Graphics")]
    public static void Configure()
    {
        Debug.Log("Configuring Android Quality and Graphics settings...");

        ConfigureQualitySettings();
        ConfigurePlayerSettings();
        ConfigureURPIfPresent();

        AssetDatabase.SaveAssets();
        Debug.Log("Android Quality & Graphics configuration complete.");
    }

    private static void ConfigureQualitySettings()
    {
        // Create or find Android quality level
        string[] qualityNames = QualitySettings.names;
        int androidQualityIndex = System.Array.FindIndex(qualityNames, q => q.ToLower().Contains("android") || q.ToLower().Contains("mobile"));
        
        if (androidQualityIndex < 0)
        {
            // Use the highest quality level available
            androidQualityIndex = qualityNames.Length - 1;
        }

        QualitySettings.SetQualityLevel(androidQualityIndex, true);

        // High quality settings for Android
        QualitySettings.antiAliasing = 4; // 4x MSAA
        QualitySettings.globalTextureMipmapLimit = 0; // Full resolution
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowDistance = 100f;

        Debug.Log($"Quality Settings configured for Android (level {androidQualityIndex}: {qualityNames[androidQualityIndex]})");
    }

    private static void ConfigurePlayerSettings()
    {
        // Color Space (Linear for better visuals)
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // Graphics APIs - prefer Vulkan for Android
        UnityEngine.Rendering.GraphicsDeviceType[] preferredAPIs = new UnityEngine.Rendering.GraphicsDeviceType[] 
        { 
            UnityEngine.Rendering.GraphicsDeviceType.Vulkan, 
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 
        };
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, preferredAPIs);

        // Multithreading
        PlayerSettings.MTRendering = true;

        Debug.Log("PlayerSettings configured: Linear color space, Vulkan+OpenGLES3, Multithreading enabled");
    }

    private static void ConfigureURPIfPresent()
    {
        // Try to find and configure URP asset
        string[] urpAssets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if (urpAssets.Length == 0)
        {
            Debug.Log("No URP asset found, skipping URP configuration.");
            return;
        }

        string urpPath = AssetDatabase.GUIDToAssetPath(urpAssets[0]);
        var urpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(urpPath);
        
        if (urpAsset == null)
        {
            Debug.LogWarning("Could not load URP asset from " + urpPath);
            return;
        }

        // High quality settings for URP (read-only properties cannot be directly set)
        // Instead, we log what quality settings we've already applied globally
        Debug.Log("URP asset found. Global Quality Settings applied (4xMSAA, VeryHigh shadows, Linear color space)");
        Debug.Log("For additional URP customization, manually edit the URP asset in Inspector:");
        Debug.Log("  - MSAA: Set to 4x in the asset or via Quality Settings");
        Debug.Log("  - Render Scale: Keep at 1.0 for full quality");
        Debug.Log("  - HDR: Enable in the asset if supported by target device");
    }
}
