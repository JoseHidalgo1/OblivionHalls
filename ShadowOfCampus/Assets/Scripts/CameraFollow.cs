using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // El jugador
    public float smoothSpeed = 10f; // Aumentado para seguimiento más rápido
    public Vector3 offset;        // Distancia de la cámara al jugador

    void LateUpdate()
    {
        if (target == null) return;

        // Posición objetivo de la cámara (mismo X,Y que el jugador)
        Vector3 targetPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        // Movimiento suave
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
