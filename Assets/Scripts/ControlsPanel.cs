using UnityEngine;
using UnityEngine.UI;

public class ControlsPanel : MonoBehaviour
{
    [Header("Texto de Controles")]
    public Text controlsText; // Asigna un Text aquí

    void Start()
    {
        if (controlsText != null)
        {
            controlsText.text = "Controles del Juego:\n\n" +
                                "Movimiento: WASD\n" +
                                "Correr: Mantén Shift\n" +
                                "Salto: Espacio\n" +
                                "Comida: Se consume cada 10 pasos\n" +
                                "Energía: Se gasta al correr, se recarga sola\n" +
                                "Salud: Pierde vida al recibir daño\n\n" +
                                "Presiona 'Volver' para regresar al menú.";
        }
    }
}