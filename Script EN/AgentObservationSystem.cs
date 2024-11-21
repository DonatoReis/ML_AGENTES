using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;

public class AgentObservationSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    public int numLowRaycasts = 7;         // Number of raycasts at low height
    public int numMediumRaycasts = 7;      // Number of raycasts at medium height
    public int numHighRaycasts = 7;        // Number of raycasts at high height
    public float rayLength = 20f;          // Length of each raycast
    public float raycastFOV = 70f;         // Field of view for raycasts
    public float rayHeightLow = 0.1f;      // Height for low raycasts
    public float rayHeightMedium = 1.0f;   // Height for medium raycasts
    public float rayHeightHigh = 2.0f;     // Height for high raycasts
    public LayerMask platformLayer;        // Layer mask for platforms
    public LayerMask wallLayer;            // Layer mask for walls
    public LayerMask groundLayer;          // Layer mask for ground

    [Header("Memory Settings")]
    public int stackedObservations = 6;    // Number of past observations to stack
    private Queue<ObservationData> observationHistory; // Queue to store observation history

    private NavigationAgentController agentController; // Reference to the agent controller
    private AgentMovement movementSystem;              // Reference to the movement system
    private AgentObjectiveSystem objectiveSystem;      // Reference to the objective system

    /// <summary>
    /// Structure to hold observation-related data.
    /// </summary>
    private struct ObservationData
    {
        public Vector3 position;         // Current position of the agent
        public Vector3 velocity;         // Current velocity of the agent
        public bool wasGrounded;         // Whether the agent was grounded in the previous state
        public float distanceToDoor;     // Distance to the door target
    }

    /// <summary>
    /// Initializes the observation system by setting up references and initializing the observation history.
    /// </summary>
    /// <param name="controller">Reference to the NavigationAgentController.</param>
    public void InitializeObservations(NavigationAgentController controller)
    {
        agentController = controller;
        movementSystem = GetComponent<AgentMovement>();
        objectiveSystem = GetComponent<AgentObjectiveSystem>();
        observationHistory = new Queue<ObservationData>();
    }

    /// <summary>
    /// Resets the observation history at the beginning of an episode.
    /// </summary>
    public void ResetObservations()
    {
        observationHistory.Clear();
    }

    /// <summary>
    /// Updates the observation history by adding the latest observation and removing the oldest if necessary.
    /// </summary>
    public void UpdateObservations()
    {
        // Ensure the observation history does not exceed the specified stack size
        if (observationHistory.Count >= stackedObservations)
        {
            observationHistory.Dequeue();
        }

        // Retrieve current movement and objective data
        var movementData = movementSystem.GetMovementData();
        var objectiveState = objectiveSystem.GetCurrentState();

        // Enqueue the latest observation data
        observationHistory.Enqueue(new ObservationData
        {
            position = movementData.position,
            velocity = movementData.velocity,
            wasGrounded = movementData.isGrounded,
            distanceToDoor = objectiveState.distanceToDoor
        });
    }

    /// <summary>
    /// Collects observations from the environment to be used by the agent.
    /// </summary>
    /// <param name="sensor">The sensor used to collect observations.</param>
    public void CollectObservations(VectorSensor sensor)
    {
        // Retrieve current movement and objective data
        var movementData = movementSystem.GetMovementData();
        var objectiveState = objectiveSystem.GetCurrentState();

        // Basic Observations
        sensor.AddObservation(movementData.position);    // Agent's current position
        sensor.AddObservation(movementData.forward);     // Agent's forward direction
        sensor.AddObservation(movementData.velocity);    // Agent's current velocity
        sensor.AddObservation(movementData.isGrounded);  // Whether the agent is grounded

        // Distances and States
        sensor.AddObservation(objectiveState.distanceToDoor);     // Distance to the door
        sensor.AddObservation(objectiveState.distanceToRoom2);    // Distance to the final objective (Room 2)
        sensor.AddObservation(objectiveState.reachedDoor);         // Whether the door has been reached

        // Raycasts at different heights
        CastRaysAtHeight(rayHeightLow, numLowRaycasts, sensor);      // Low height raycasts
        CastRaysAtHeight(rayHeightMedium, numMediumRaycasts, sensor);// Medium height raycasts
        CastRaysAtHeight(rayHeightHigh, numHighRaycasts, sensor);    // High height raycasts

        // Relative Directions
        sensor.AddObservation(objectiveState.directionToDoor);       // Direction to the door
        sensor.AddObservation(objectiveState.directionToRoom2);      // Direction to the final objective

        // Observation History
        foreach (var obs in observationHistory)
        {
            sensor.AddObservation(obs.position);          // Past positions
            sensor.AddObservation(obs.velocity);          // Past velocities
            sensor.AddObservation(obs.wasGrounded);       // Past grounded states
            sensor.AddObservation(obs.distanceToDoor);     // Past distances to the door
        }
    }

    /// <summary>
    /// Casts a series of raycasts at a specified height and adds their observations to the sensor.
    /// </summary>
    /// <param name="height">The height at which to cast the rays.</param>
    /// <param name="numRays">The number of rays to cast.</param>
    /// <param name="sensor">The sensor to add observations to.</param>
    private void CastRaysAtHeight(float height, int numRays, VectorSensor sensor)
    {
        Vector3 rayStart = transform.position + Vector3.up * height; // Starting position of the raycasts
        float angleStep = raycastFOV / (numRays - 1);               // Angle between each ray
        float startAngle = -raycastFOV / 2;                         // Starting angle for the first ray

        for (int i = 0; i < numRays; i++)
        {
            float angle = startAngle + i * angleStep;               // Current angle for this ray
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward; // Direction of the ray
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(rayStart, direction, out hit, rayLength, platformLayer | wallLayer | groundLayer))
            {
                sensor.AddObservation(hit.distance / rayLength);       // Normalized distance to the hit
                sensor.AddObservation(hit.collider.gameObject.layer);  // Layer of the hit object
            }
            else
            {
                sensor.AddObservation(1.0f); // No hit within ray length
                sensor.AddObservation(0);     // Default layer value
            }
        }
    }

    /// <summary>
    /// Visualizes the raycasts and observation areas in the Unity Editor for debugging purposes.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualize raycast directions and hits
        DrawRaycasts(rayHeightLow, numLowRaycasts, Color.red);
        DrawRaycasts(rayHeightMedium, numMediumRaycasts, Color.yellow);
        DrawRaycasts(rayHeightHigh, numHighRaycasts, Color.blue);
    }

    /// <summary>
    /// Helper method to draw raycasts in the Unity Editor for visualization.
    /// </summary>
    /// <param name="height">The height at which to cast the rays.</param>
    /// <param name="numRays">The number of rays to cast.</param>
    /// <param name="color">The color to use for the ray lines.</param>
    private void DrawRaycasts(float height, int numRays, Color color)
    {
        Vector3 rayStart = transform.position + Vector3.up * height;
        float angleStep = raycastFOV / (numRays - 1);
        float startAngle = -raycastFOV / 2;

        Gizmos.color = color;

        for (int i = 0; i < numRays; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(rayStart, direction * rayLength);
        }
    }
}
