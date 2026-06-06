using UnityEngine;

[DefaultExecutionOrder(-50)]
public class MobileHUDVisibility : MonoBehaviour
{
    void Awake()
    {
        if (!Application.isMobilePlatform)
        {
            gameObject.SetActive(false);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HideMobileHUDOnNonMobilePlatforms()
    {
        if (Application.isMobilePlatform)
            return;

        // Hide any prefabs or objects that belong to the mobile HUD.
        // This covers existing scenes where the root was added without the MobileHUDVisibility component.
        var joystickComponents = FindObjectsOfType<global::VirtualJoystick>(true);
        foreach (var joystick in joystickComponents)
        {
            if (joystick != null)
                joystick.transform.root.gameObject.SetActive(false);
        }

        var mobileButtons = FindObjectsOfType<global::MobileButton>(true);
        foreach (var button in mobileButtons)
        {
            if (button != null)
                button.transform.root.gameObject.SetActive(false);
        }

        var explicitRoot = GameObject.Find("MobileHUD_Root");
        if (explicitRoot != null)
        {
            explicitRoot.SetActive(false);
        }
    }
}
