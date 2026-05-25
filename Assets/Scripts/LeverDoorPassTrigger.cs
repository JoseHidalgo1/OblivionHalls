using UnityEngine;

public class LeverDoorPassTrigger : MonoBehaviour
{
    public LeverDoor parentLeverDoor;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (parentLeverDoor != null)
        {
            parentLeverDoor.HandlePlayerPassTriggerEnter(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (parentLeverDoor != null)
        {
            parentLeverDoor.HandlePlayerPassTriggerExit(collision);
        }
    }
}
