using UnityEngine;
using System.Collections;

public class TargetPlatform : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color activatedColor = Color.green;

    [Header("Movement Settings")]
    [SerializeField] private float loweringDistance = 0.3f;
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float waitTime = 10f;

    [Header("Door Settings")]
    [SerializeField] private Door door;

    private MeshRenderer meshRenderer;
    private Vector3 initialPosition;
    private Vector3 loweredPosition;
    private bool isInContact = false;
    private Coroutine movementCoroutine;
    private Coroutine timeCoroutine;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip floorSound;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found! Disabling the script.");
            enabled = false;
            return;
        }

        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = normalColor;

        initialPosition = transform.position;
        loweredPosition = initialPosition - new Vector3(0, loweringDistance, 0);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            ActivatePlatform();

            if (audioSource != null && floorSound != null)
            {
                audioSource.PlayOneShot(floorSound);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            isInContact = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("albert"))
        {
            DeactivatePlatform();
        }
    }

    private void ActivatePlatform()
    {
        if (isInContact) return;

        isInContact = true;
        StopCoroutines();

        meshRenderer.material.color = activatedColor;

        movementCoroutine = StartCoroutine(MovePlatform(transform.position, loweredPosition));

        door?.OpenDoor();
    }

    private void DeactivatePlatform()
    {
        isInContact = false;
        timeCoroutine = StartCoroutine(WaitAndDeactivate());
    }

    private IEnumerator WaitAndDeactivate()
    {
        yield return new WaitForSeconds(waitTime);

        if (!isInContact)
        {
            meshRenderer.material.color = normalColor;
            movementCoroutine = StartCoroutine(MovePlatform(transform.position, initialPosition));
            door?.CloseDoor();
        }
    }

    private IEnumerator MovePlatform(Vector3 startPosition, Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }

    private void StopCoroutines()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            timeCoroutine = null;
        }
    }
}
