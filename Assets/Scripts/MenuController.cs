using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject controlsPanel; // Asigna el panel de controles aquí

    public void PlayGame()
    {
        // Cambia "GameScene" por el nombre real de tu escena de juego
        SceneManager.LoadScene("GameScene");
    }

    public void ShowControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }

    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
