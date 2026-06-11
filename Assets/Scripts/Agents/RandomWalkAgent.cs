using UnityEngine;

public class RandomWalkAgent : MonoBehaviour
{
    public enum Action
    {
        MoveForward,
        TurnLeft,
        TurnRight,
        Wait
    }

    [SerializeField] float stepDistance = 1.5f;
    [SerializeField] float turnDegrees = 90f;
    [SerializeField] float sensorDistance = 2.5f;
    [SerializeField] float wallTurnBias = 0.75f;
    [SerializeField] float moveForwardWeight = 0.4f;
    [SerializeField] float turnLeftWeight = 0.2f;
    [SerializeField] float turnRightWeight = 0.2f;
    [SerializeField] float bodyRadius = 0.35f;

    System.Random _rng;
    MazeGenerator _generator;
    bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int CollisionCount { get; private set; }
    public Action LastAction { get; private set; } = Action.Wait;

    public void BeginEpisode(int mazeSeed, MazeGenerator generator)
    {
        _rng = new System.Random(mazeSeed + 90401);
        _generator = generator;
        CollisionCount = 0;
        _enabled = true;
    }

    public void EndEpisode()
    {
        _enabled = false;
        _generator = null;
    }

    public void ExecuteStep()
    {
        if (!_enabled || _rng == null || _generator == null)
            return;

        LastAction = ChooseAction();
        ApplyAction(LastAction);
    }

    Action ChooseAction()
    {
        if (IsWallAhead(sensorDistance))
        {
            if (_rng.NextDouble() < wallTurnBias)
                return _rng.NextDouble() < 0.5 ? Action.TurnLeft : Action.TurnRight;

            return Action.Wait;
        }

        double roll = _rng.NextDouble();
        if (roll < moveForwardWeight)
            return Action.MoveForward;
        if (roll < moveForwardWeight + turnLeftWeight)
            return Action.TurnLeft;
        if (roll < moveForwardWeight + turnLeftWeight + turnRightWeight)
            return Action.TurnRight;

        return Action.Wait;
    }

    void ApplyAction(Action action)
    {
        switch (action)
        {
            case Action.MoveForward:
                TryMoveForward();
                break;
            case Action.TurnLeft:
                transform.Rotate(0f, -turnDegrees, 0f);
                break;
            case Action.TurnRight:
                transform.Rotate(0f, turnDegrees, 0f);
                break;
        }
    }

    void TryMoveForward()
    {
        Vector3 origin = SensorOrigin();
        Vector3 direction = transform.forward;

        if (_generator.TryRaycastVisibleWall(origin, direction, stepDistance + bodyRadius, out float hitDistance))
        {
            CollisionCount++;
            float safeDistance = Mathf.Max(0f, hitDistance - bodyRadius);
            if (safeDistance > 0.01f)
                transform.position += direction * safeDistance;
            return;
        }

        transform.position += direction * stepDistance;
    }

    bool IsWallAhead(float distance)
    {
        return _generator.TryRaycastVisibleWall(
            SensorOrigin(),
            transform.forward,
            distance + bodyRadius,
            out _);
    }

    Vector3 SensorOrigin()
    {
        return transform.position + Vector3.up * 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        if (!_enabled)
            return;

        Gizmos.color = Color.yellow;
        Vector3 origin = SensorOrigin();
        Gizmos.DrawLine(origin, origin + transform.forward * sensorDistance);
    }
}
