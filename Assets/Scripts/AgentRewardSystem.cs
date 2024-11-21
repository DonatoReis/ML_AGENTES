using UnityEngine;

public class AgentRewardSystem : MonoBehaviour
{
    [Header("Reward Settings")]
    public float fastPlateReward = 1.0f;
    public float mediumPlateReward = 0.9f;
    public float slowPlateReward = 0.8f;
    public float explorationReward = 0.0f;
    public float fallPenalty = -0.5f;
    public float wallPenalty = -0.1f;
    public float groundPenalty = -0.05f;
    public float timePenalty = -0.1f;

    [Header("Reward for Escaping the Room")]
    public float escapeRoomReward = 1.0f;
    public GameObject escapeTarget; // Target set in the Inspector
    public LayerMask goalLayer;     // Platform layer

    private NavigationAgentController agentController;
    private float episodeStartTime;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    private Vector3 lastPosition;
    private float inactivityTimer = 0f;
    private const float inactivityThreshold = 5f; // Time in seconds

    // Variables to control rewards and penalties
    private float frameReward = 0f;
    private float timeSinceLastPenalty = 0f;
    private float timeSinceLastExplorationReward = 0f;

    public void InitializeRewards(NavigationAgentController controller)
    {
        agentController = controller;
        lastPosition = transform.position;

        // Subscribe to the reward event
        agentController.OnAddReward += OnAddReward;
    }

    public void Update()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.1f)
        {
            inactivityTimer += Time.deltaTime;
            if (inactivityTimer >= inactivityThreshold)
            {
                agentController.AddReward(-0.01f); // Penalize for inactivity
                inactivityTimer = 0f; // Reset the timer
            }
        }
        else
        {
            inactivityTimer = 0f; // Reset if the agent moves
        }

        lastPosition = transform.position;

        // Print the accumulated reward of the current frame if significant
        if (Mathf.Abs(frameReward) > 0.05f)
        {
            Debug.Log($"Reward in frame {Time.frameCount}: {frameReward}");
        }
        frameReward = 0f;
    }

    public void ResetRewards()
    {
        episodeStartTime = Time.time;
        frameReward = 0f;
        timeSinceLastPenalty = 0f;
        timeSinceLastExplorationReward = 0f;
    }

    public void ProcessRewards(AgentObjectiveSystem.ObjectiveState objectiveState, AgentMovement.MovementData movementData)
    {
        float timeInEpisode = Time.time - episodeStartTime;

        // Reward based on time to reach all objectives
        if (objectiveState.visitedGoalsCount == objectiveState.totalGoals)
        {
            if (timeInEpisode < 5f)
                agentController.AddReward(fastPlateReward);
            else if (timeInEpisode < 10f)
                agentController.AddReward(mediumPlateReward);
            else
                agentController.AddReward(slowPlateReward);

            agentController.EndEpisode();
        }
        else
        {
            // Exploration reward applied every second
            timeSinceLastExplorationReward += Time.deltaTime;
            if (timeSinceLastExplorationReward >= 1f)
            {
                agentController.AddReward(explorationReward);
                timeSinceLastExplorationReward = 0f;
            }
        }

        // Time penalty applied every second
        timeSinceLastPenalty += Time.deltaTime;
        if (timeSinceLastPenalty >= 1f)
        {
            agentController.AddReward(-timePenalty);
            timeSinceLastPenalty = 0f;
        }

        // Penalties for falling
        if (movementData.position.y < -1f)
        {
            agentController.AddReward(fallPenalty);
            agentController.EndEpisode();
        }
    }

    public void ProcessCollision(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            agentController.AddReward(wallPenalty);
        }
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            agentController.AddReward(groundPenalty);
        }
        else if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            // Penalty for colliding with an obstacle
            agentController.AddReward(groundPenalty);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the agent reached the escapeTarget or collided with the goalLayer
        if (other.gameObject == escapeTarget || ((1 << other.gameObject.layer) & goalLayer) != 0)
        {
            agentController.AddReward(escapeRoomReward);
            // Remove or comment out the line below to prevent the agent from being reset
            // agentController.EndEpisode();
            Debug.Log("Agent escaped the room! Reward granted.");
        }
    }

    // Method called when a reward is added
    public void OnAddReward(float reward)
    {
        frameReward += reward;
    }

    private void OnDestroy()
    {
        if (agentController != null)
        {
            agentController.OnAddReward -= OnAddReward;
        }
    }
}
