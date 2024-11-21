using UnityEngine;

public class AgentRewardSystem : MonoBehaviour
{
    [Header("Reward Settings")]
    public float fastPlateReward = 1.0f;          // Reward for reaching the door quickly
    public float mediumPlateReward = 0.9f;        // Reward for reaching the door in a medium time
    public float slowPlateReward = 0.8f;          // Reward for reaching the door slowly
    public float escapeRoomReward = 1.0f;         // Reward for escaping the room
    public float fallPenalty = -0.5f;             // Penalty for falling
    public float wallPenalty = -0.1f;             // Penalty for colliding with a wall
    public float groundPenalty = -0.05f;          // Penalty for colliding with the ground
    public float timePenalty = -0.001f;           // Penalty based on time spent in the episode
    public float exitMovementReward = 0.005f;     // Reward for moving towards the exit

    private NavigationAgentController agentController; // Reference to the agent controller
    private float episodeStartTime;                     // Timestamp when the episode started
    public LayerMask wallLayer;                         // Layer mask for walls
    public LayerMask groundLayer;                       // Layer mask for ground

    /// <summary>
    /// Initializes the reward system by setting up references.
    /// </summary>
    /// <param name="controller">Reference to the NavigationAgentController.</param>
    public void InitializeRewards(NavigationAgentController controller)
    {
        agentController = controller;
    }

    /// <summary>
    /// Resets the reward system at the beginning of an episode.
    /// </summary>
    public void ResetRewards()
    {
        episodeStartTime = Time.time;
    }

    /// <summary>
    /// Processes rewards based on the current objective state and movement data.
    /// </summary>
    /// <param name="objectiveState">The current state of objectives.</param>
    /// <param name="movementData">The current movement data of the agent.</param>
    public void ProcessRewards(AgentObjectiveSystem.ObjectiveState objectiveState, AgentMovement.MovementData movementData)
    {
        float timeInEpisode = Time.time - episodeStartTime;

        // Rewards based on objectives
        if (objectiveState.reachedDoor && !objectiveState.previouslyReachedDoor)
        {
            if (timeInEpisode < 5f)
                agentController.AddReward(fastPlateReward);
            else if (timeInEpisode < 10f)
                agentController.AddReward(mediumPlateReward);
            else
                agentController.AddReward(slowPlateReward);
        }

        // Rewards for movement
        if (!objectiveState.reachedDoor)
        {
            // Penalize based on the distance to the door and time
            agentController.AddReward(-objectiveState.distanceToDoor * timePenalty);

            // Reward for moving towards the door
            if (Vector3.Dot(movementData.velocity, objectiveState.directionToDoor) > 0)
            {
                agentController.AddReward(exitMovementReward);
            }
        }
        else
        {
            // Penalize based on the distance to the final objective and time
            agentController.AddReward(-objectiveState.distanceToRoom2 * timePenalty);
        }

        // Penalties
        if (movementData.position.y < -1f)
        {
            // Apply fall penalty and end the episode if the agent falls below a certain point
            agentController.AddReward(fallPenalty);
            agentController.EndEpisode();
        }

        // Apply a small time penalty each step to encourage faster completion
        agentController.AddReward(timePenalty);
    }

    /// <summary>
    /// Processes rewards based on collision events.
    /// </summary>
    /// <param name="collision">Details about the collision event.</param>
    public void ProcessCollision(Collision collision)
    {
        if (collision.gameObject.layer == wallLayer)
        {
            // Apply wall collision penalty
            agentController.AddReward(wallPenalty);
        }
        else if (collision.gameObject.layer == groundLayer)
        {
            // Apply ground collision penalty
            agentController.AddReward(groundPenalty);
        }
    }
}
