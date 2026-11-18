
using UnityEngine;

public class PlayerStartPosition : MonoBehaviour
{
    [Header("Starting Position")]
    public Vector3 startPosition = new Vector3(-2.344423f, 2.81f, -4.14f);

    private void Start()
    {
        // Temporarily disable CharacterController
        CharacterController controller = GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        // Move player to starting position
        transform.position = startPosition;

        // Re-enable CharacterController
        if (controller != null)
            controller.enabled = true;
    }
}
