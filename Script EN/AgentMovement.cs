using UnityEngine;
using Unity.MLAgents.Actuators;

public class AgentMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;          // Speed at which the agent moves forward/backward
    public float jumpForce = 7f;          // Force applied when the agent jumps
    public float rotateSpeed = 80f;       // Speed at which the agent rotates

    [Header("Jump Settings")]
    public float groundCheckRadius = 0.2f; // Radius for ground detection
    public LayerMask groundLayer;          // Layer mask for identifying the ground
    public LayerMask platformLayer;        // Layer mask for identifying platforms
    public float maxJumpCooldown = 0.5f;   // Cooldown time between jumps

    private Rigidbody rb;                  // Reference to the Rigidbody component
    private Vector3 startPosition;         // Starting position of the agent
    private bool isGrounded;               // Flag to check if the agent is on the ground
    private float lastJumpTime;            // Timestamp of the last jump
    private NavigationAgentController agentController; // Reference to the agent controller
    private RaycastHit groundHit;          // Information about the ground hit

    /// <summary>
    /// Structure to hold movement-related data.
    /// </summary>
    public struct MovementData
    {
        public Vector3 position;    // Current position of the agent
        public Vector3 velocity;    // Current velocity of the agent
        public bool isGrounded;     // Whether the agent is on the ground
        public Vector3 forward;     // Forward direction of the agent
    }

    /// <summary>
    /// Initializes the movement system by setting up references and configuring the Rigidbody.
    /// </summary>
    /// <param name="controller">Reference to the NavigationAgentController.</param>
    public void InitializeMovement(NavigationAgentController controller)
    {
        agentController = controller;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        lastJumpTime = -maxJumpCooldown;

        // Configure Rigidbody constraints and properties
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.mass = 1f;
        rb.linearDamping = 1f;
    }

    /// <summary>
    /// Resets the agent's movement by repositioning and resetting velocities.
    /// </summary>
    public void ResetMovement()
    {
        // Reset position and rotation to start values
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        
        // Reset velocities to zero
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Updates the grounded state of the agent by performing a sphere cast.
    /// </summary>
    public void UpdateMovement()
    {
        // Perform a sphere cast to check if the agent is grounded
        isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * groundCheckRadius,
            groundCheckRadius,
            Vector3.down,
            out groundHit,
            groundCheckRadius * 2,
            groundLayer | platformLayer
        );
    }

    /// <summary>
    /// Processes the actions received from the agent's policy to control movement.
    /// </summary>
    /// <param name="actions">The actions to be processed.</param>
    public void ProcessActions(ActionBuffers actions)
    {
        float moveForward = actions.ContinuousActions[0]; // Movement input
        float rotate = actions.ContinuousActions[1];      // Rotation input
        bool jump = actions.DiscreteActions[0] == 1;      // Jump input

        // Movement: Apply forward/backward movement based on input
        Vector3 movement = transform.forward * moveForward * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Rotation: Rotate the agent around the Y-axis based on input
        transform.Rotate(0, rotate * rotateSpeed * Time.fixedDeltaTime, 0);

        // Jump: Apply jump force if jump input is received and conditions are met
        if (jump && isGrounded && Time.time > lastJumpTime + maxJumpCooldown)
        {
            Vector3 jumpDirection = (Vector3.up + transform.forward * moveForward * 0.5f).normalized;
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }
    }

    /// <summary>
    /// Provides a heuristic for manual control of the agent, useful for debugging.
    /// </summary>
    /// <param name="actionsOut">The output actions based on user input.</param>
    public void ProcessHeuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Forward/Backward movement based on W/S keys
        continuousActionsOut[0] = Input.GetKey(KeyCode.W) ? 1.0f :
                                   Input.GetKey(KeyCode.S) ? -1.0f : 0.0f;

        // Rotation based on A/D keys
        continuousActionsOut[1] = Input.GetKey(KeyCode.A) ? -1.0f :
                                   Input.GetKey(KeyCode.D) ? 1.0f : 0.0f;

        // Jump based on Space key
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    /// <summary>
    /// Retrieves the current movement data of the agent.
    /// </summary>
    /// <returns>A MovementData struct containing the agent's movement information.</returns>
    public MovementData GetMovementData()
    {
        return new MovementData
        {
            position = transform.position,
            velocity = rb.linearVelocity,
            isGrounded = isGrounded,
            forward = transform.forward
        };
    }

    /// <summary>
    /// Visualizes the ground check sphere in the Unity Editor for debugging purposes.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Set color based on whether the agent is grounded
            Gizmos.color = isGrounded ? Color.green : Color.red;
            
            // Draw a wireframe sphere to represent the ground check area
            Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckRadius, groundCheckRadius);
        }
    }
}
