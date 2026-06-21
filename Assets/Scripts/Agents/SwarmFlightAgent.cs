using System.Collections.Generic;
using UnityEngine;

public struct SwarmFlightSettings
{
    public Vector3 ArenaCenter;
    public Vector3 ArenaSize;
    public float NeighborRadius;
    public float SeparationRadius;
    public float MaxSpeed;
    public float MaxForce;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float CohesionWeight;
    public float WanderWeight;
    public float BoundaryWeight;
    public float BoundaryMargin;
    public MazeGenerator Maze;
    public float MazeWallAvoidanceDistance;
    public float MazeWallAvoidanceWeight;
}

public class SwarmFlightAgent : MonoBehaviour
{
    Vector3 _velocity;
    bool _enabled;

    public int AgentIndex { get; private set; }
    public Vector3 Velocity => _velocity;
    public int BoundaryHits { get; private set; }

    public void BeginEpisode(int agentIndex, Vector3 initialVelocity)
    {
        AgentIndex = agentIndex;
        _velocity = initialVelocity;
        BoundaryHits = 0;
        _enabled = true;
        OrientToVelocity();
    }

    public void EndEpisode()
    {
        _enabled = false;
    }

    public void ExecuteStep(
        IReadOnlyList<SwarmFlightAgent> agents,
        SwarmFlightSettings settings,
        float deltaTime,
        System.Random rng)
    {
        if (!_enabled || agents == null || rng == null || deltaTime <= 0f)
            return;

        Vector3 steering =
            ComputeSeparation(agents, settings) * settings.SeparationWeight +
            ComputeAlignment(agents, settings) * settings.AlignmentWeight +
            ComputeCohesion(agents, settings) * settings.CohesionWeight +
            RandomUnitVector(rng) * settings.WanderWeight +
            ComputeBoundaryAvoidance(settings) * settings.BoundaryWeight +
            ComputeMazeWallAvoidance(settings) * settings.MazeWallAvoidanceWeight;

        steering = Vector3.ClampMagnitude(steering, settings.MaxForce);
        _velocity = Vector3.ClampMagnitude(_velocity + steering * deltaTime, settings.MaxSpeed);

        if (_velocity.sqrMagnitude < 0.01f)
            _velocity = RandomUnitVector(rng) * Mathf.Max(0.1f, settings.MaxSpeed * 0.25f);

        transform.position += _velocity * deltaTime;
        ClampInsideArena(settings);
        OrientToVelocity();
    }

    Vector3 ComputeSeparation(IReadOnlyList<SwarmFlightAgent> agents, SwarmFlightSettings settings)
    {
        Vector3 force = Vector3.zero;
        int count = 0;

        for (int i = 0; i < agents.Count; i++)
        {
            SwarmFlightAgent other = agents[i];
            if (other == null || other == this)
                continue;

            Vector3 offset = transform.position - other.transform.position;
            float distance = offset.magnitude;
            if (distance <= 0.001f || distance > settings.SeparationRadius)
                continue;

            force += offset.normalized / distance;
            count++;
        }

        return count > 0 ? force / count : Vector3.zero;
    }

    Vector3 ComputeAlignment(IReadOnlyList<SwarmFlightAgent> agents, SwarmFlightSettings settings)
    {
        Vector3 average = Vector3.zero;
        int count = 0;

        for (int i = 0; i < agents.Count; i++)
        {
            SwarmFlightAgent other = agents[i];
            if (other == null || other == this)
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > settings.NeighborRadius)
                continue;

            average += other.Velocity;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        average /= count;
        return average.sqrMagnitude > 0.001f ? average.normalized : Vector3.zero;
    }

    Vector3 ComputeCohesion(IReadOnlyList<SwarmFlightAgent> agents, SwarmFlightSettings settings)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < agents.Count; i++)
        {
            SwarmFlightAgent other = agents[i];
            if (other == null || other == this)
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > settings.NeighborRadius)
                continue;

            center += other.transform.position;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        center /= count;
        Vector3 desired = center - transform.position;
        return desired.sqrMagnitude > 0.001f ? desired.normalized : Vector3.zero;
    }

    Vector3 ComputeBoundaryAvoidance(SwarmFlightSettings settings)
    {
        Vector3 local = transform.position - settings.ArenaCenter;
        Vector3 half = settings.ArenaSize * 0.5f;
        float margin = Mathf.Max(0.1f, settings.BoundaryMargin);
        Vector3 force = Vector3.zero;

        force.x += AxisBoundaryForce(local.x, half.x, margin);
        force.y += AxisBoundaryForce(local.y, half.y, margin);
        force.z += AxisBoundaryForce(local.z, half.z, margin);
        return force;
    }

    Vector3 ComputeMazeWallAvoidance(SwarmFlightSettings settings)
    {
        if (settings.Maze == null || settings.MazeWallAvoidanceDistance <= 0f)
            return Vector3.zero;

        if (!settings.Maze.TryWorldToCell(transform.position, out int cellX, out int cellY))
            return Vector3.zero;

        float cellSize = settings.Maze.Config.cellSize;
        Vector3 cellCenter = settings.Maze.GetCellCenterWorld(cellX, cellY);
        float halfCell = cellSize * 0.5f;
        float margin = Mathf.Min(settings.MazeWallAvoidanceDistance, halfCell);
        Vector3 offset = transform.position - cellCenter;
        Vector3 force = Vector3.zero;

        AddCellWallForce(settings.Maze, cellX, cellY, 1, 0, halfCell - offset.x, margin, Vector3.left, ref force);
        AddCellWallForce(settings.Maze, cellX, cellY, -1, 0, halfCell + offset.x, margin, Vector3.right, ref force);
        AddCellWallForce(settings.Maze, cellX, cellY, 0, 1, halfCell - offset.z, margin, Vector3.back, ref force);
        AddCellWallForce(settings.Maze, cellX, cellY, 0, -1, halfCell + offset.z, margin, Vector3.forward, ref force);
        return force;
    }

    static void AddCellWallForce(
        MazeGenerator maze,
        int cellX,
        int cellY,
        int dirX,
        int dirZ,
        float distanceToWall,
        float margin,
        Vector3 awayFromWall,
        ref Vector3 force)
    {
        if (distanceToWall > margin)
            return;

        if (!maze.HasVisibleWallBetween(cellX, cellY, dirX, dirZ))
            return;

        float strength = 1f - Mathf.Clamp01(distanceToWall / Mathf.Max(0.001f, margin));
        force += awayFromWall * strength;
    }

    static float AxisBoundaryForce(float value, float halfExtent, float margin)
    {
        float distanceToPositive = halfExtent - value;
        if (distanceToPositive < margin)
            return -Mathf.Clamp01(1f - distanceToPositive / margin);

        float distanceToNegative = halfExtent + value;
        if (distanceToNegative < margin)
            return Mathf.Clamp01(1f - distanceToNegative / margin);

        return 0f;
    }

    void ClampInsideArena(SwarmFlightSettings settings)
    {
        Vector3 half = settings.ArenaSize * 0.5f;
        Vector3 min = settings.ArenaCenter - half;
        Vector3 max = settings.ArenaCenter + half;
        Vector3 position = transform.position;
        bool hit = false;

        ClampAxis(ref position.x, ref _velocity.x, min.x, max.x, ref hit);
        ClampAxis(ref position.y, ref _velocity.y, min.y, max.y, ref hit);
        ClampAxis(ref position.z, ref _velocity.z, min.z, max.z, ref hit);

        if (hit)
            BoundaryHits++;

        transform.position = position;
    }

    static void ClampAxis(ref float position, ref float velocity, float min, float max, ref bool hit)
    {
        if (position < min)
        {
            position = min;
            velocity = Mathf.Abs(velocity);
            hit = true;
        }
        else if (position > max)
        {
            position = max;
            velocity = -Mathf.Abs(velocity);
            hit = true;
        }
    }

    void OrientToVelocity()
    {
        if (_velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
    }

    static Vector3 RandomUnitVector(System.Random rng)
    {
        float x = (float)(rng.NextDouble() * 2.0 - 1.0);
        float y = (float)(rng.NextDouble() * 2.0 - 1.0);
        float z = (float)(rng.NextDouble() * 2.0 - 1.0);
        Vector3 value = new Vector3(x, y, z);
        return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 6f);
    }
}
