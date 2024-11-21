using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class NavigationAgentController : Agent
{
    [Header("Components")]
    private AgentMovement movementSystem;
    private AgentRewardSystem rewardSystem;
    private AgentObservationSystem observationSystem;
    private AgentObjectiveSystem objectiveSystem;

    /// <summary>
    /// Initializes and configures all the systems used by the agent.
    /// </summary>
    public override void Initialize()
    {
        // Adds and configures all necessary components to the agent
        movementSystem = gameObject.AddComponent<AgentMovement>();
        rewardSystem = gameObject.AddComponent<AgentRewardSystem>();
        observationSystem = gameObject.AddComponent<AgentObservationSystem>();
        objectiveSystem = gameObject.AddComponent<AgentObjectiveSystem>();

        // Initializes each system with the current agent instance
        movementSystem.InitializeMovement(this);
        rewardSystem.InitializeRewards(this);
        observationSystem.InitializeObservations(this);
        objectiveSystem.InitializeObjectives(this);
    }

    /// <summary>
    /// Resets the agent and all systems at the beginning of each episode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Resets movement parameters
        movementSystem.ResetMovement();
        
        // Resets reward tracking
        rewardSystem.ResetRewards();
        
        // Clears previous observations
        observationSystem.ResetObservations();
        
        // Resets objectives for the new episode
        objectiveSystem.ResetObjectives();
    }

    /// <summary>
    /// Collects observations from the environment to be used by the agent.
    /// </summary>
    /// <param name="sensor">The sensor used to collect observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Delegates the collection of observations to the observation system
        observationSystem.CollectObservations(sensor);
    }

    /// <summary>
    /// Processes the actions received from the agent's policy.
    /// </summary>
    /// <param name="actions">The actions to be processed.</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Processes movement based on the received actions
        movementSystem.ProcessActions(actions);

        // Checks if the agent has achieved any objectives
        objectiveSystem.CheckObjectives();
        
        // Processes rewards based on the current state and movement data
        rewardSystem.ProcessRewards(
            objectiveSystem.GetCurrentState(),
            movementSystem.GetMovementData()
        );
    }

    /// <summary>
    /// Provides a heuristic for manual control of the agent, useful for debugging.
    /// </summary>
    /// <param name="actionsOut">The output actions based on user input.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Processes manual input to control movement
        movementSystem.ProcessHeuristic(in actionsOut);
    }

    /// <summary>
    /// Updates the agent's movement and observations at fixed intervals.
    /// </summary>
    private void FixedUpdate()
    {
        // Updates movement parameters
        movementSystem.UpdateMovement();
        
        // Updates observations based on the latest state
        observationSystem.UpdateObservations();
    }

    /// <summary>
    /// Handles collision events and processes any resulting rewards or penalties.
    /// </summary>
    /// <param name="collision">Details about the collision event.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // Processes rewards or penalties based on the collision
        rewardSystem.ProcessCollision(collision);
    }
}
