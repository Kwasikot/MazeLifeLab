using System;
using UnityEngine;

public class Experiment001Runner : MonoBehaviour
{
    [SerializeField] MazeGen mazeGen;
    [SerializeField] MazeCellIndex startCell = new MazeCellIndex(0, 0);
    [SerializeField] MazeCellIndex goalCell = new MazeCellIndex(19, 19);
    [SerializeField] int maxSteps = 5000;
    [SerializeField] float goalRadius = 2f;
    [SerializeField] float goalVisualScale = 8f;
    [SerializeField] float agentHeight = 0.5f;
    [SerializeField] Transform agent;
    [SerializeField] Transform goalMarker;
    [SerializeField] bool autoStartOnPlay = true;

    ManualAgentController _manualController;
    Vector3 _goalWorldPosition;
    int _steps;
    bool _isRunning;

    public int Steps => _steps;
    public bool IsRunning => _isRunning;
    public bool Success { get; private set; }
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();
    }

    void OnEnable()
    {
        if (mazeGen != null)
            mazeGen.OnMazeRegenerated += HandleMazeRegenerated;
    }

    void OnDisable()
    {
        if (mazeGen != null)
            mazeGen.OnMazeRegenerated -= HandleMazeRegenerated;
    }

    void Start()
    {
        if (autoStartOnPlay)
            BeginEpisode();
    }

    void FixedUpdate()
    {
        if (!_isRunning || agent == null)
            return;

        _steps++;

        if (HorizontalDistance(agent.position, _goalWorldPosition) <= goalRadius)
        {
            EndEpisode(EpisodeTerminationReason.Success, success: true);
            return;
        }

        if (_steps >= maxSteps)
            EndEpisode(EpisodeTerminationReason.Timeout, success: false);
    }

    void HandleMazeRegenerated()
    {
        if (Application.isPlaying)
            BeginEpisode();
    }

    [ContextMenu("Begin Episode")]
    public void BeginEpisode()
    {
        if (!EnsureMazeReady())
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);

        if (!ValidateEpisodeSetup(generator, resolvedGoal))
            return;

        EnsureAgentExists();
        EnsureGoalMarkerExists();
        EnsureRuntimeObjectsAreSeparate();

        _goalWorldPosition = generator.GetCellCenterWorld(resolvedGoal.x, resolvedGoal.y);
        Vector3 startPosition = generator.GetCellCenterWorld(startCell.x, startCell.y);
        startPosition.y = agentHeight;
        _goalWorldPosition.y = agentHeight * 0.5f;

        agent.SetParent(null, true);
        goalMarker.SetParent(null, true);

        agent.position = startPosition;
        agent.rotation = Quaternion.identity;
        goalMarker.position = _goalWorldPosition;

        _steps = 0;
        _isRunning = true;
        Success = false;
        TerminationReason = EpisodeTerminationReason.None;
        SetManualControlEnabled(true);
        EnsureCameraFollow(true);

        Debug.Log(
            $"[EXP-001] episode started seed={generator.MazeSeed} " +
            $"start={startCell} ({startPosition.x:F1},{startPosition.z:F1}) " +
            $"goal={resolvedGoal} ({_goalWorldPosition.x:F1},{_goalWorldPosition.z:F1}) " +
            $"maxSteps={maxSteps}. Press F to toggle camera: full maze / follow agent.");
    }

    [ContextMenu("Reset Episode")]
    public void ResetEpisode()
    {
        BeginEpisode();
    }

    bool EnsureMazeReady()
    {
        if (mazeGen == null)
        {
            Debug.LogError("[EXP-001] MazeGen reference is missing.");
            return false;
        }

        if (!mazeGen.HasGeneratedMaze)
            mazeGen.Regenerate();

        return mazeGen.HasGeneratedMaze;
    }

    MazeCellIndex ResolveGoalCell(MazeGenerator generator)
    {
        if (goalCell.x >= 0 && goalCell.y >= 0)
            return goalCell;

        int lastX = generator.Config.mazeWidthCells - 1;
        int lastY = generator.Config.mazeHeightCells - 1;
        return new MazeCellIndex(lastX, lastY);
    }

    bool ValidateEpisodeSetup(MazeGenerator generator, MazeCellIndex resolvedGoal)
    {
        if (!generator.IsCellInBounds(startCell.x, startCell.y) ||
            !generator.IsCellInBounds(resolvedGoal.x, resolvedGoal.y))
        {
            Debug.LogError(
                $"[EXP-001] Invalid start or goal cell. start={startCell} goal={resolvedGoal}");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        if (startCell.x == resolvedGoal.x && startCell.y == resolvedGoal.y)
        {
            Debug.LogError("[EXP-001] Start cell and goal cell must be different.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        return true;
    }

    void EnsureAgentExists()
    {
        if (agent != null && !IsValidAgentTransform(agent))
        {
            Debug.LogWarning("[EXP-001] Invalid agent reference; creating a dedicated Agent object.");
            agent = null;
        }

        if (agent != null)
        {
            _manualController = agent.GetComponent<ManualAgentController>();
            if (_manualController == null)
                _manualController = agent.gameObject.AddComponent<ManualAgentController>();
            RemoveColliderIfPresent(agent.gameObject);
            DisableShadows(agent.gameObject);
            return;
        }

        var agentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agentObject.name = "Agent";
        agentObject.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        RemoveColliderIfPresent(agentObject);
        agent = agentObject.transform;
        _manualController = agentObject.AddComponent<ManualAgentController>();

        var renderer = agentObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
            DisableShadows(agentObject);
        }
    }

    void EnsureGoalMarkerExists()
    {
        if (goalMarker != null && goalMarker == agent)
            goalMarker = null;

        if (goalMarker == null)
        {
            var goalObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goalObject.name = "GoalMarker";
            goalMarker = goalObject.transform;
        }

        ApplyGoalVisual(goalMarker.gameObject);
    }

    void ApplyGoalVisual(GameObject goalObject)
    {
        float scale = goalVisualScale > 0f ? goalVisualScale : 8f;
        goalObject.transform.localScale = new Vector3(scale * 0.75f, scale * 0.5f, scale * 0.75f);
        RemoveColliderIfPresent(goalObject);

        var renderer = goalObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(0.1f, 1f, 0.2f);
            DisableShadows(goalObject);
        }
    }

    static void DisableShadows(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void EnsureRuntimeObjectsAreSeparate()
    {
        if (agent == null || goalMarker == null)
            return;

        if (agent == goalMarker)
        {
            Debug.LogError("[EXP-001] Agent and goal cannot be the same object.");
            goalMarker = null;
            EnsureGoalMarkerExists();
        }

        if (goalMarker.IsChildOf(agent))
            goalMarker.SetParent(null, true);

        if (agent.IsChildOf(goalMarker))
            agent.SetParent(null, true);
    }

    static bool IsValidAgentTransform(Transform candidate)
    {
        if (candidate.GetComponent<MazeGen>() != null ||
            candidate.GetComponent<Experiment001Runner>() != null)
        {
            return false;
        }

        return candidate.name == "Agent" || candidate.GetComponent<ManualAgentController>() != null;
    }

    static void RemoveColliderIfPresent(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    void EnsureCameraFollow(bool showFullMaze)
    {
        if (agent == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<Experiment001CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<Experiment001CameraFollow>();

        follow.Target = agent;
        follow.Height = 80f;
        follow.ConfigureForMaze(
            mazeGen.Config.mazeWidthCells,
            mazeGen.Config.mazeHeightCells,
            mazeGen.Config.cellSize);

        if (showFullMaze)
            follow.ShowFullMazeView(snapImmediately: true);
        else
            follow.FollowAgent(snapImmediately: true);
    }

    void EndEpisode(EpisodeTerminationReason reason, bool success)
    {
        if (!_isRunning && reason != EpisodeTerminationReason.InvalidConfiguration)
            return;

        _isRunning = false;
        Success = success;
        TerminationReason = reason;
        SetManualControlEnabled(false);

        Debug.Log(
            $"[EXP-001] episode ended success={success} steps={_steps} " +
            $"reason={reason} seed={mazeGen.Generator.MazeSeed}");
    }

    void SetManualControlEnabled(bool enabled)
    {
        if (_manualController != null)
            _manualController.ControlEnabled = enabled;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmosSelected()
    {
        if (mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);

        Gizmos.color = Color.blue;
        Vector3 start = generator.GetCellCenterWorld(startCell.x, startCell.y);
        start.y = agentHeight;
        Gizmos.DrawWireSphere(start, 0.75f);

        Gizmos.color = Color.green;
        Vector3 goal = generator.GetCellCenterWorld(resolvedGoal.x, resolvedGoal.y);
        goal.y = agentHeight * 0.5f;
        Gizmos.DrawWireSphere(goal, goalRadius);
    }
}
