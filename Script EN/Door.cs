using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float openHeight = 4f;       // Height to which the door opens
    [SerializeField] private float movementSpeed = 5f;   // Speed at which the door moves

    private Vector3 closedPosition;                       // Position of the door when closed
    private Vector3 openPosition;                         // Position of the door when open
    private Coroutine movementCoroutine;                  // Reference to the current movement coroutine

    /// <summary>
    /// Initializes the door's closed and open positions at the start.
    /// </summary>
    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
    }

    /// <summary>
    /// Opens the door by moving it to the open position.
    /// </summary>
    public void OpenDoor()
    {
        MoveTo(openPosition);
    }

    /// <summary>
    /// Closes the door by moving it back to the closed position.
    /// </summary>
    public void CloseDoor()
    {
        MoveTo(closedPosition);
    }

    /// <summary>
    /// Initiates the movement of the door to the target position.
    /// Stops any ongoing movement before starting a new one.
    /// </summary>
    /// <param name="targetPosition">The position to which the door should move.</param>
    private void MoveTo(Vector3 targetPosition)
    {
        // If a movement coroutine is already running, stop it
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        // Start a new movement coroutine towards the target position
        movementCoroutine = StartCoroutine(MoveDoor(targetPosition));
    }

    /// <summary>
    /// Coroutine that smoothly moves the door to the target position.
    /// </summary>
    /// <param name="targetPosition">The position to which the door should move.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        // Continue moving until the door is close enough to the target position
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // Move the door towards the target position at the specified speed
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // Ensure the door is exactly at the target position once movement is complete
        transform.position = targetPosition;
    }
}
