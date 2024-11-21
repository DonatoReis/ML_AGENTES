using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float openingHeight = 4f;
    [SerializeField] private float movementSpeed = 5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip doorMovementSound;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private bool useContinuousSound = true;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine movementCoroutine;
    private bool isDoorOpen = false;
    private AudioSource audioSource;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openingHeight;

        // Configure the AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Basic AudioSource settings
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; // Partially 3D sound for better audibility
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 50f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = soundVolume;
    }

    public void OpenDoor()
    {
        Debug.Log("OpenDoor called."); // Log to verify execution
        if (!isDoorOpen)
        {
            MoveTo(openPosition);
            isDoorOpen = true;

            // Play the open sound
            if (doorOpenSound != null)
            {
                Debug.Log("Playing door open sound.");
                audioSource.PlayOneShot(doorOpenSound, soundVolume);
            }
        }
    }

    public void CloseDoor()
    {
        Debug.Log("CloseDoor called."); // Log to verify execution
        if (isDoorOpen)
        {
            MoveTo(closedPosition);
            isDoorOpen = false;

            // Play the close sound
            if (doorCloseSound != null)
            {
                Debug.Log("Playing door close sound.");
                audioSource.PlayOneShot(doorCloseSound, soundVolume);
            }
        }
    }

    private void MoveTo(Vector3 targetPosition)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        movementCoroutine = StartCoroutine(MoveDoor(targetPosition));
    }

    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        float initialDistance = Vector3.Distance(transform.position, targetPosition);

        if (useContinuousSound && doorMovementSound != null)
        {
            Debug.Log("Playing continuous movement sound.");
            audioSource.clip = doorMovementSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;

        if (useContinuousSound && audioSource.isPlaying && doorMovementSound != null)
        {
            Debug.Log("Stopping continuous movement sound.");
            audioSource.Stop();
        }
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
