using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(AgentMovement))]
[RequireComponent(typeof(AgentRewardSystem))]
[RequireComponent(typeof(AgentObservationSystem))]
[RequireComponent(typeof(AgentObjectiveSystem))]
public class NavigationAgentController : Agent
{
    [Header("Required Components")]
    [SerializeField] public AgentMovement movementSystem;
    [SerializeField] public AgentRewardSystem rewardSystem;
    [SerializeField] public AgentObservationSystem observationSystem;
    [SerializeField] public AgentObjectiveSystem objectiveSystem;
    [SerializeField] private SpawnAreaManager spawnManager;

    public int lessonNumber;

    // Event to notify when a reward is added
    public delegate void AddRewardDelegate(float reward);
    public event AddRewardDelegate OnAddReward;

    protected override void Awake()
    {
        base.Awake();
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (movementSystem == null)
            movementSystem = GetComponent<AgentMovement>();
        if (rewardSystem == null)
            rewardSystem = GetComponent<AgentRewardSystem>();
        if (observationSystem == null)
            observationSystem = GetComponent<AgentObservationSystem>();
        if (objectiveSystem == null)
            objectiveSystem = GetComponent<AgentObjectiveSystem>();
        if (spawnManager == null)
            spawnManager = GetComponentInParent<SpawnAreaManager>();

        // Check and warn about missing components
        if (spawnManager == null)
            Debug.LogError("SpawnAreaManager not found in the scene! Please add a SpawnAreaManager.");

        // Pass the reference to the ObjectiveSystem
        if (objectiveSystem != null && spawnManager != null)
            objectiveSystem.spawnManager = spawnManager;
    }

    public override void Initialize()
    {
        ValidateComponents();

        movementSystem.InitializeMovement(this);
        rewardSystem.InitializeRewards(this);
        observationSystem.InitializeObservations(this);
        objectiveSystem.InitializeObjectives(this);
    }

    public override void OnEpisodeBegin()
    {
        // Set fixed values for parameters
        float maxJumpHeight = 5.0f; // Desired value for maximum jump height
        bool allowMovement = true; // Allow movement
        bool allowJump = true;     // Allow jumping
        bool exploreRoom = true;   // Allow room exploration
        bool hasObjective = true;  // The agent has an objective

        // Configure the agent and environment based on these parameters
        movementSystem.SetMovementAllowed(allowMovement);
        movementSystem.SetJumpAllowed(allowJump);
        movementSystem.SetMaxJumpHeight(maxJumpHeight);

        objectiveSystem.SetExplorationAllowed(exploreRoom);
        objectiveSystem.SetObjectiveActive(hasObjective);

        // Reset the systems
        movementSystem.ResetMovement();
        rewardSystem.ResetRewards();
        observationSystem.ResetObservations();
        objectiveSystem.ResetObjectives();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observationSystem.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movementSystem.ProcessActions(actions);
        objectiveSystem.CheckObjectives();
        rewardSystem.ProcessRewards(
            objectiveSystem.GetCurrentState(),
            movementSystem.GetMovementData()
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        movementSystem.ProcessHeuristic(in actionsOut);
    }

    private void FixedUpdate()
    {
        movementSystem.UpdateMovement();
        observationSystem.UpdateObservations();
    }

    private void OnCollisionEnter(Collision collision)
    {
        rewardSystem.ProcessCollision(collision);
    }

    // Override the AddReward method to notify the reward system
    public new void AddReward(float reward)
    {
        base.AddReward(reward);
        OnAddReward?.Invoke(reward);
    }

    private void OnDestroy()
    {
        // Remove event subscriptions to prevent leaks
        if (rewardSystem != null)
        {
            OnAddReward -= rewardSystem.OnAddReward;
        }
    }
}
