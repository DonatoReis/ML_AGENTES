using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AgentObjectiveSystem : MonoBehaviour
{
    [Header("Objetivos")]
    public Transform GoalsParent;
    public List<GameObject> allGoals = new List<GameObject>();
    private List<GameObject> visitedGoals = new List<GameObject>();

    // Propriedades para acessar os contadores
    public int totalGoals { get { return allGoals.Count; } }
    public int visitedGoalsCount { get { return visitedGoals.Count; } }

    [Header("Configurações")]
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

    // Variáveis para o Curriculum Learning
    private bool explorationAllowed = true;
    private bool objectiveActive = true;

    // **Nova variável pública para o objetivo específico**
    [Header("Configurações do Objetivo Específico")]
    public GameObject specificGoal; // Arraste o objetivo específico no Inspector

    // Flag para controlar se o reset por tempo está habilitado
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

        // Resetar a flag no início do episódio
        timeResetEnabled = true;
    }

    private void InitializeGoals()
    {
        // Limpa as listas
        allGoals.Clear();
        visitedGoals.Clear();

        // Encontra todos os objetivos com a tag "Goal" que são filhos do GoalsParent
        if (GoalsParent != null)
        {
            foreach (Transform child in GoalsParent)
            {
                if (child.CompareTag("Goal"))
                {
                    allGoals.Add(child.gameObject);
                    // Opcional: Reiniciar o estado visual do objetivo, se necessário
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
        // **Modificação para verificar se o reset por tempo está habilitado**
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

        // Verifica se a porta deve ser aberta
        if (visitedGoals.Count == allGoals.Count - 1)
        {
            // Abre a porta
            Door door = FindFirstObjectByType<Door>();
            if (door != null)
            {
                door.OpenDoor();
            }
        }

        // Verifica se todos os objetivos foram visitados
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

    // Métodos para o Curriculum Learning
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
        Debug.Log("OnTriggerEnter chamado com " + other.gameObject.name);

        if (other.CompareTag("Goal"))
        {
            if (!visitedGoals.Contains(other.gameObject))
            {
                visitedGoals.Add(other.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Recompensa de 0.5f adicionada por visitar o objetivo: {other.gameObject.name}");

                // **Verifica se o objetivo específico foi alcançado**
                if (other.gameObject == specificGoal)
                {
                    timeResetEnabled = false; // Desabilita o reset por tempo
                    Debug.Log("Objetivo específico alcançado. Reset por tempo desabilitado.");
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionEnter chamado com " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Goal"))
        {
            if (!visitedGoals.Contains(collision.gameObject))
            {
                visitedGoals.Add(collision.gameObject);
                agentController.AddReward(0.5f);
                Debug.Log($"Recompensa de 0.5f adicionada por visitar o objetivo: {collision.gameObject.name}");

                // **Verifica se o objetivo específico foi alcançado**
                if (collision.gameObject == specificGoal)
                {
                    timeResetEnabled = false; // Desabilita o reset por tempo
                    Debug.Log("Objetivo específico alcançado. Reset por tempo desabilitado.");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualiza os objetivos
        Gizmos.color = Color.yellow;
        foreach (var goal in allGoals)
        {
            Gizmos.DrawWireSphere(goal.transform.position, goalReachDistance);
        }

        // Mostra o tempo restante
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
