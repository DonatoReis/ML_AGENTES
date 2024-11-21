using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AgentObjectiveSystem : MonoBehaviour
{
    [Header("Objectives")]
    public Transform GoalsParent;
    public List<GameObject> allGoals = new List<GameObject>();
    private List<GameObject> visitedGoals = new List<GameObject>();

    // Properties to access the counters
    public int totalGoals { get { return allGoals.Count; } }
    public int visitedGoalsCount { get { return visitedGoals.Count; } }

    [Header("Settings")]
    public float goalReachDistance = 1.5f;

    [Header("Timer Settings")]
    public float maxTimeInRoom = 30f;
    private float currentRoomTime;

    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text generationText;
    private int currentGeneration = 0;

    [Header("Spawn Settings")]
    public SpawnAreaManager spawnManager;

    private NavigationAgentController agentController;

    // Variables for Curriculum Learning
    private bool explorationAllowed = true;
    private bool objectiveActive = true;

    // **New public variable for the specific objective**
    [Header("Specific Objective Settings")]
    public GameObject specificGoal; // Drag the specific objective in the Inspector

    // Flag to control if time reset is enabled
    private bool timeResetEnabled = true;

    public struct ObjectiveState
    {
        public float timeInRoom;
        public bool explorationAllowed;
        public int totalGoals;
        public int visitedGoalsCount;
    }

    public void InitializeObjectives(NavigationAgentController controller)
    {
        agentController = controller;
        ResetTimer();
        UpdateUI();
        InitializeGoals();

        // Reset the flag at the start of the episode
        timeResetEnabled = true;
    }

    private void InitializeGoals()
    {
        // Clear the lists
        allGoals.Clear();
        visitedGoals.Clear();

        // Find all goals with the tag "Goal" that are children of GoalsParent
        if (GoalsParent != null)
        {
            foreach (Transform child in GoalsParent)
            {
                if (child.CompareTag("Goal"))
                {
                    allGoals.Add(child.gameObject);
                    // Optional: Reset the visual state of the goal, if necessary
                }
            }
        }
    }

    private void ResetTimer()
    {
        currentRoomTime = 0f;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            int remainingTime = Mathf.CeilToInt(maxTimeInRoom - currentRoomTime);
            timerText.text = remainingTime.ToString("00");
        }

        if (generationText != null)
        {
            generationText.text = currentGeneration.ToString("00");
        }
    }

    public void ResetObjectives()
    {
        ResetTimer();
        currentGeneration++;
        visitedGoals.Clear();

        if (spawnManager != null)
        {
            spawnManager.RespawnAgent(gameObject);
        }

        UpdateUI();
    }

    public void CheckObjectives()
    {
        // **Modification to check if time reset is enabled**
        if (timeResetEnabled)
        {
            currentRoomTime += Time.deltaTime;
            UpdateUI();

            if (currentRoomTime >= maxTimeInRoom)
            {
                agentController.AddReward(-0.5f);
                agentController.EndEpisode();
                //currentGeneration++;
                ResetTimer();
                UpdateUI();
                return;
            }
        }

        // Check if the door should be opened
        if (visitedGoals.Count == allGoals.Count - 1)
        {
            // Open the door
            Door door = FindFirstObjectByType<Door>();
            if (door != null)
            {
                door.OpenDoor();
            }
        }

        // Check if all goals have been visited
        if (visitedGoals.Count == allGoals.Count)
        {
            agentController.AddReward(1.0f);
            agentController.EndEpisode();
        }
    }

    public ObjectiveState GetCurrentState()
    {
        return new ObjectiveState
        {
            timeInRoom = currentRoomTime,
            explorationAllowed = explorationAllowed,
            totalGoals = allGoals.Count,
            visitedGoalsCount = visitedGoals.Count
        };
    }

    // Methods for Curriculum Learning
    public void SetExplorationAllowed(bool allowed)
    {
        explorationAllowed = allowed;
    }

    public void SetObjectiveActive(bool active)
    {
        objectiveActive = active;
    }

    public void ResetGenerationCount()
    {
        currentGeneration = 0;
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter called with " + other.gameObject.name);

        if (other.CompareTag("Goal"))
        {
            if (!visitedGoals.Contains(other.gameObject))
            {
                visitedGoals.Add(other.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Added a reward of 0.5f for visiting the goal: {other.gameObject.name}");

                // **Check if the specific goal was reached**
                if (other.gameObject == specificGoal)
                {
                    timeResetEnabled = false; // Disable time reset
                    Debug.Log("Specific goal reached. Time reset disabled.");
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter called with " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Goal"))
        {
            if (!visitedGoals.Contains(collision.gameObject))
            {
                visitedGoals.Add(collision.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Added a reward of 0.5f for visiting the goal: {collision.gameObject.name}");

                // **Check if the specific goal was reached**
                if (collision.gameObject == specificGoal)
                {
                    timeResetEnabled = false; // Disable time reset
                    Debug.Log("Specific goal reached. Time reset disabled.");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualize the goals
        Gizmos.color = Color.yellow;
        foreach (var goal in allGoals)
        {
            Gizmos.DrawWireSphere(goal.transform.position, goalReachDistance);
        }

        // Show the remaining time
        float remainingTime = maxTimeInRoom - currentRoomTime;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"Time: {remainingTime:F1}s");
    }

    private void OnValidate()
    {
        maxTimeInRoom = Mathf.Max(1f, maxTimeInRoom);
        goalReachDistance = Mathf.Max(0.1f, goalReachDistance);
    }
}
