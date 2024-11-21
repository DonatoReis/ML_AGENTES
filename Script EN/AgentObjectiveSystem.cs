using UnityEngine;

public class AgentObjectiveSystem : MonoBehaviour
{
    [Header("Objectives")]
    public Transform doorTarget;         // Target representing the door
    public Transform room2Target;        // Target representing the final objective (Room 2)

    [Header("Settings")]
    public float doorReachDistance = 1.5f;  // Distance within which the door is considered reached
    public float room2ReachDistance = 1.5f; // Distance within which the final objective is considered reached

    private bool reachedDoor = false;            // Flag to indicate if the door has been reached
    private NavigationAgentController agentController; // Reference to the agent controller

    /// <summary>
    /// Structure to hold the current state of objectives.
    /// </summary>
    public struct ObjectiveState
    {
        public float distanceToDoor;             // Current distance to the door
        public float distanceToRoom2;            // Current distance to the final objective (Room 2)
        public bool reachedDoor;                 // Whether the door has been reached
        public bool previouslyReachedDoor;       // Whether the door was previously reached
        public Vector3 directionToDoor;          // Direction vector pointing towards the door
        public Vector3 directionToRoom2;         // Direction vector pointing towards the final objective
    }

    /// <summary>
    /// Initializes the objective system by setting up references.
    /// </summary>
    /// <param name="controller">Reference to the NavigationAgentController.</param>
    public void InitializeObjectives(NavigationAgentController controller)
    {
        agentController = controller;
    }

    /// <summary>
    /// Resets the objective system at the beginning of an episode.
    /// </summary>
    public void ResetObjectives()
    {
        reachedDoor = false;
    }

    /// <summary>
    /// Checks if the agent has reached any of the objectives and handles episode termination.
    /// </summary>
    public void CheckObjectives()
    {
        var state = GetCurrentState();
        bool previouslyReachedDoor = reachedDoor;

        // Check if the agent has reached the door
        if (!reachedDoor && state.distanceToDoor < doorReachDistance)
        {
            reachedDoor = true;
        }

        // Check if the agent has completed the final objective (reached Room 2) after reaching the door
        if (reachedDoor && state.distanceToRoom2 < room2ReachDistance)
        {
            agentController.EndEpisode();
        }
    }

    /// <summary>
    /// Retrieves the current state of the objectives.
    /// </summary>
    /// <returns>An ObjectiveState struct containing the current state information.</returns>
    public ObjectiveState GetCurrentState()
    {
        Vector3 currentPosition = transform.position;

        // Calculate distances to the door and final objective
        float distanceToDoor = Vector3.Distance(currentPosition, doorTarget.position);
        float distanceToRoom2 = Vector3.Distance(currentPosition, room2Target.position);

        // Calculate direction vectors towards the door and final objective
        Vector3 directionToDoor = (doorTarget.position - currentPosition).normalized;
        Vector3 directionToRoom2 = (room2Target.position - currentPosition).normalized;

        return new ObjectiveState
        {
            distanceToDoor = distanceToDoor,
            distanceToRoom2 = distanceToRoom2,
            reachedDoor = reachedDoor,
            previouslyReachedDoor = reachedDoor, // This can be expanded if tracking previous states is needed
            directionToDoor = directionToDoor,
            directionToRoom2 = directionToRoom2
        };
    }

    /// <summary>
    /// Visualizes objective reach areas and direction vectors in the Unity Editor for debugging purposes.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualize the door reach area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(doorTarget.position, doorReachDistance);

        // Visualize the final objective (Room 2) reach area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(room2Target.position, room2ReachDistance);

        // Draw direction lines from the agent to the door and final objective
        if (Application.isPlaying)
        {
            var state = GetCurrentState();

            // Direction line to the door
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, state.directionToDoor * 2f);

            // Direction line to the final objective
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, state.directionToRoom2 * 2f);
        }
    }
}
