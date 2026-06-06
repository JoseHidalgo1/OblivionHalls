using UnityEngine;

public static class MobileRuntimeSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Create a persistent manager object to hold mobile runtime helpers
        GameObject mgr = GameObject.Find("_MobileRuntimeManager");
        if (mgr == null)
        {
            mgr = new GameObject("_MobileRuntimeManager");
            Object.DontDestroyOnLoad(mgr);
            mgr.AddComponent<MobileInputInitializer>();
            mgr.AddComponent<ForceLandscape>();
        }
    }
}
