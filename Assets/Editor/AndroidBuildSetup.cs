using UnityEditor;
using UnityEngine;

public static class AndroidBuildSetup
{
    [MenuItem("Build/Configure Android Settings")]
    public static void Configure()
    {
        Debug.Log("Configuring recommended Android PlayerSettings...");

        // Set a placeholder bundle identifier — change this to your own package name
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yourcompany.oblivionhalls");

        // Set version
        PlayerSettings.bundleVersion = "1.0.0";

        // Use Gradle
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        // Preferred scripting backend IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        // Prefer ARM64
        #if UNITY_2019_3_OR_NEWER
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        #endif

        // Target API level: use highest installed (Auto). For Play Store you should target latest stable API.
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // Default internet permission
        PlayerSettings.Android.forceInternetPermission = true;

        Debug.Log("Android PlayerSettings configured. Review values (bundle id, keystore) before building.");
    }
}
