using UnityEngine;
using System.Collections;

public class TargetPlatform : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.red;        // Color when the platform is inactive
    [SerializeField] private Color activatedColor = Color.green;   // Color when the platform is activated

    [Header("Movement Settings")]
    [SerializeField] private float loweringDistance = 0.3f;        // Distance to lower the platform
    [SerializeField] private float movementSpeed = 2f;             // Speed at which the platform moves
    [SerializeField] private float waitTime = 5f;                  // Time to wait before deactivating the platform

    [Header("Door Settings")]
    [SerializeField] private Door door;                             // Reference to the Door script

    private MeshRenderer meshRenderer;                             // Reference to the platform's MeshRenderer
    private Vector3 initialPosition;                               // Initial position of the platform
    private Vector3 loweredPosition;                               // Lowered position of the platform
    private bool isInContact = false;                              // Flag to check if the player is in contact with the platform
    private Coroutine movementCoroutine;                           // Reference to the current movement coroutine
    private Coroutine waitCoroutine;                               // Reference to the current wait coroutine

    [Header("Audio Settings")]
    [SerializeField] private AudioClip floorSound;                  // Sound to play when the platform is activated
    [SerializeField] private AudioSource audioSource;               // Audio source component for playing sounds

    /// <summary>
    /// Initializes the platform's properties at the start.
    /// </summary>
    private void Start()
    {
        // Get the MeshRenderer component attached to the platform
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found! Disabling the script.");
            enabled = false;
            return;
        }

        // Create a new material instance to avoid modifying the shared material
        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = normalColor; // Set the initial color to normal

        // Set the initial and lowered positions based on the current position and lowering distance
        initialPosition = transform.position;
        loweredPosition = initialPosition - new Vector3(0, loweringDistance, 0);

        // Ensure the AudioSource component is assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Prevent the audio from playing automatically
        }
    }

    /// <summary>
    /// Called when another collider enters the platform's collider.
    /// Activates the platform if the colliding object is tagged as "albert".
    /// </summary>
    /// <param name="collision">Details about the collision event.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            ActivatePlatform();

            // Play the floor sound if the audio source and clip are assigned
            if (audioSource != null && floorSound != null)
            {
                audioSource.PlayOneShot(floorSound);
            }
        }
    }

    /// <summary>
    /// Called once per frame for every collider that is touching the platform's collider.
    /// Maintains the contact state if the colliding object is tagged as "albert".
    /// </summary>
    /// <param name="collision">Details about the collision event.</param>
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            isInContact = true;
        }
    }

    /// <summary>
    /// Called when another collider stops touching the platform's collider.
    /// Deactivates the platform if the colliding object is tagged as "albert".
    /// </summary>
    /// <param name="collision">Details about the collision event.</param>
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            DeactivatePlatform();
        }
    }

    /// <summary>
    /// Activates the platform by changing its color, moving it to the lowered position,
    /// and opening the associated door.
    /// </summary>
    private void ActivatePlatform()
    {
        if (isInContact) return; // Prevent multiple activations

        isInContact = true;
        StopCoroutines(); // Stop any ongoing coroutines to prevent conflicts

        meshRenderer.material.color = activatedColor; // Change the platform color to activated

        // Start moving the platform to the lowered position
        movementCoroutine = StartCoroutine(MovePlatform(transform.position, loweredPosition));

        // Open the associated door if it exists
        door?.OpenDoor();
    }

    /// <summary>
    /// Deactivates the platform by initiating a wait before resetting its position and color,
    /// and closing the associated door.
    /// </summary>
    private void DeactivatePlatform()
    {
        isInContact = false;
        waitCoroutine = StartCoroutine(WaitAndDeactivate());
    }

    /// <summary>
    /// Waits for a specified time before deactivating the platform.
    /// If the platform is not in contact, it resets its position and color and closes the door.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator WaitAndDeactivate()
    {
        yield return new WaitForSeconds(waitTime); // Wait for the specified duration

        if (!isInContact)
        {
            meshRenderer.material.color = normalColor; // Reset the platform color to normal
            movementCoroutine = StartCoroutine(MovePlatform(transform.position, initialPosition)); // Move back to the initial position
            door?.CloseDoor(); // Close the associated door if it exists
        }
    }

    /// <summary>
    /// Coroutine that smoothly moves the platform from the starting position to the target position.
    /// </summary>
    /// <param name="startPosition">The starting position of the platform.</param>
    /// <param name="targetPosition">The target position to move the platform to.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator MovePlatform(Vector3 startPosition, Vector3 targetPosition)
    {
        // Continue moving until the platform is close enough to the target position
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // Move the platform towards the target position at the specified speed
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        transform.position = targetPosition; // Ensure the platform is exactly at the target position
    }

    /// <summary>
    /// Stops any ongoing movement or wait coroutines to prevent conflicts.
    /// </summary>
    private void StopCoroutines()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }
}
