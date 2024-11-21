using UnityEngine;

public class AgentRewardSystem : MonoBehaviour
{
    [Header("Configurações de Recompensas")]
    public float recompensaPlacaRapida = 1.0f;
    public float recompensaPlacaMedia = 0.9f;
    public float recompensaPlacaLenta = 0.8f;
    public float recompensaExploracao = 0.0f;
    public float penalizacaoQueda = -0.5f;
    public float penalizacaoParede = -0.1f;
    public float penalizacaoChao = -0.05f;
    public float penalizacaoPorTempo = -0.1f;

    [Header("Recompensa por Escapar da Sala")]
    public float recompensaEscaparSala = 1.0f;
    public GameObject escapeTarget; // Alvo definido no Inspector
    public LayerMask goalLayer;     // Layer da plataforma

    private NavigationAgentController agentController;
    private float episodeStartTime;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    private Vector3 lastPosition;
    private float inactivityTimer = 0f;
    private const float inactivityThreshold = 5f; // Tempo em segundos

    // Variáveis para controle de recompensas e penalizações
    private float frameReward = 0f;
    private float timeSinceLastPenalty = 0f;
    private float timeSinceLastExplorationReward = 0f;

    public void InitializeRewards(NavigationAgentController controller)
    {
        agentController = controller;
        lastPosition = transform.position;

        // Subscrição ao evento de recompensa
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
                agentController.AddReward(-0.01f); // Penaliza por inatividade
                inactivityTimer = 0f; // Reseta o temporizador
            }
        }
        else
        {
            inactivityTimer = 0f; // Reseta se o agente se mover
        }

        lastPosition = transform.position;

        // Imprime a recompensa acumulada do frame atual se for significativa
        if (Mathf.Abs(frameReward) > 0.05f)
        {
            Debug.Log($"Recompensa no frame {Time.frameCount}: {frameReward}");
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

        // Recompensa baseada no tempo para alcançar todos os objetivos
        if (objectiveState.visitedGoalsCount == objectiveState.totalGoals)
        {
            if (timeInEpisode < 5f)
                agentController.AddReward(recompensaPlacaRapida);
            else if (timeInEpisode < 10f)
                agentController.AddReward(recompensaPlacaMedia);
            else
                agentController.AddReward(recompensaPlacaLenta);

            agentController.EndEpisode();
        }
        else
        {
            // Recompensa por exploração aplicada a cada segundo
            timeSinceLastExplorationReward += Time.deltaTime;
            if (timeSinceLastExplorationReward >= 1f)
            {
                agentController.AddReward(recompensaExploracao);
                timeSinceLastExplorationReward = 0f;
            }
        }

        // Penalização por tempo aplicada a cada segundo
        timeSinceLastPenalty += Time.deltaTime;
        if (timeSinceLastPenalty >= 1f)
        {
            agentController.AddReward(-penalizacaoPorTempo);
            timeSinceLastPenalty = 0f;
        }

        // Penalizações por cair
        if (movementData.position.y < -1f)
        {
            agentController.AddReward(penalizacaoQueda);
            agentController.EndEpisode();
        }
    }

    public void ProcessCollision(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            agentController.AddReward(penalizacaoParede);
        }
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            agentController.AddReward(penalizacaoChao);
        }
        else if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            // Penalização por colidir com obstáculo
            agentController.AddReward(penalizacaoChao);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o agente alcançou o escapeTarget ou colidiu com a goalLayer
        if (other.gameObject == escapeTarget || ((1 << other.gameObject.layer) & goalLayer) != 0)
        {
            agentController.AddReward(recompensaEscaparSala);
            // Remover ou comentar a linha abaixo para que o agente não seja resetado
            // agentController.EndEpisode();
            Debug.Log("Agente escapou da sala! Recompensa concedida.");
        }
    }

    // Método chamado quando uma recompensa é adicionada
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
