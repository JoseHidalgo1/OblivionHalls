using UnityEngine;

[DefaultExecutionOrder(-200)]
public class ForceLandscape : MonoBehaviour
{
    void Awake()
    {
        // Disable portrait rotations
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        // Set orientation to landscape immediately
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }
}
