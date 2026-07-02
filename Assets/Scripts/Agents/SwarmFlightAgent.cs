using System.Collections.Generic;
using UnityEngine;

public struct SwarmFlightSettings
{
    public Vector3 ArenaCenter;
    public Vector3 ArenaSize;
    public IReadOnlyList<SwarmFlightObstacle> Obstacles;
    public ISwarmExplorationField ExplorationField;
    public ISwarmForagingField ForagingField;
    public ISwarmScentField ScentField;
    public ISwarmFoodCoordinationField CoordinationField;
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
    public float ObstacleAvoidanceDistance;
    public float ObstacleAvoidanceWeight;
    public float ExplorationProbeDistance;
    public float ExplorationWeight;
    public float ForagingWeight;
    public float ScentWeight;
    public float CoordinationWeight;
}

public interface ISwarmFoodCoordinationField
{
    bool TrySampleCoordinationDirection(
        int agentIndex,
        Vector3 position,
        Vector3 velocity,
        out Vector3 direction);
}

public struct SwarmFlightObstacle
{
    public Vector3 Center;
    public float Radius;
    public float Height;
    public Vector2 HalfExtentsXZ;

    public SwarmFlightObstacle(Vector3 center, float radius, float height)
    {
        Center = center;
        Radius = radius;
        Height = height;
        HalfExtentsXZ = new Vector2(radius, radius);
    }

    public SwarmFlightObstacle(Vector3 center, Vector2 halfExtentsXZ, float height)
    {
        Center = center;
        HalfExtentsXZ = new Vector2(
            Mathf.Max(0.1f, halfExtentsXZ.x),
            Mathf.Max(0.1f, halfExtentsXZ.y));
        Radius = Mathf.Max(HalfExtentsXZ.x, HalfExtentsXZ.y);
        Height = height;
    }
}

public interface ISwarmExplorationField
{
    bool TrySampleNoveltyDirection(
        Vector3 position,
        Vector3 velocity,
        float probeDistance,
        out Vector3 direction);
}

public interface ISwarmForagingField
{
    bool TrySampleForagingDirection(
        int agentIndex,
        Vector3 position,
        Vector3 velocity,
        out Vector3 direction);
}

public interface ISwarmScentField
{
    bool TrySampleScentDirection(
        int agentIndex,
        Vector3 position,
        Vector3 velocity,
        out Vector3 direction);
}

public class SwarmFlightAgent : MonoBehaviour
{
    Vector3 _velocity;
    bool _enabled;

    public int AgentIndex { get; private set; }
    public Vector3 Velocity => _velocity;
    public int BoundaryHits { get; private set; }
    public bool LastUsedExplorationBias { get; private set; }
    public int ExplorationBiasSteps { get; private set; }
    public bool LastUsedScentBias { get; private set; }
    public int ScentBiasSteps { get; private set; }
    public bool LastUsedCoordinationBias { get; private set; }
    public int CoordinationBiasSteps { get; private set; }
    public int LastCoordinationFoodTarget { get; private set; } = -1;

    public void BeginEpisode(int agentIndex, Vector3 initialVelocity)
    {
        AgentIndex = agentIndex;
        _velocity = initialVelocity;
        BoundaryHits = 0;
        LastUsedExplorationBias = false;
        ExplorationBiasSteps = 0;
        LastUsedScentBias = false;
        ScentBiasSteps = 0;
        LastUsedCoordinationBias = false;
        CoordinationBiasSteps = 0;
        LastCoordinationFoodTarget = -1;
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

        LastUsedExplorationBias = false;
        LastUsedScentBias = false;
        LastUsedCoordinationBias = false;
        LastCoordinationFoodTarget = -1;
        Vector3 steering =
            ComputeSeparation(agents, settings) * settings.SeparationWeight +
            ComputeAlignment(agents, settings) * settings.AlignmentWeight +
            ComputeCohesion(agents, settings) * settings.CohesionWeight +
            RandomUnitVector(rng) * settings.WanderWeight +
            ComputeBoundaryAvoidance(settings) * settings.BoundaryWeight +
            ComputeMazeWallAvoidance(settings) * settings.MazeWallAvoidanceWeight +
            ComputeObstacleAvoidance(settings) * settings.ObstacleAvoidanceWeight +
            ComputeExplorationBias(settings) * settings.ExplorationWeight +
            ComputeForagingBias(settings) * settings.ForagingWeight +
            ComputeScentBias(settings) * settings.ScentWeight +
            ComputeCoordinationBias(settings) * settings.CoordinationWeight;

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

    Vector3 ComputeObstacleAvoidance(SwarmFlightSettings settings)
    {
        if (settings.Obstacles == null || settings.ObstacleAvoidanceDistance <= 0f)
            return Vector3.zero;

        Vector3 force = Vector3.zero;
        Vector3 position = transform.position;

        for (int i = 0; i < settings.Obstacles.Count; i++)
        {
            SwarmFlightObstacle obstacle = settings.Obstacles[i];
            float halfHeight = Mathf.Max(0.1f, obstacle.Height * 0.5f);
            float verticalDistance = Mathf.Abs(position.y - obstacle.Center.y);
            if (verticalDistance > halfHeight + settings.ObstacleAvoidanceDistance)
                continue;

            Vector3 away = ObstacleAvoidanceDirection(position, obstacle, out float surfaceDistance);
            if (surfaceDistance > settings.ObstacleAvoidanceDistance)
                continue;

            float horizontalStrength = 1f - Mathf.Clamp01(surfaceDistance / settings.ObstacleAvoidanceDistance);
            float verticalStrength = 1f - Mathf.Clamp01(Mathf.Max(0f, verticalDistance - halfHeight) / settings.ObstacleAvoidanceDistance);
            force += away * horizontalStrength * verticalStrength;
        }

        return force;
    }

    Vector3 ComputeExplorationBias(SwarmFlightSettings settings)
    {
        if (settings.ExplorationField == null || settings.ExplorationWeight <= 0f)
            return Vector3.zero;

        if (!settings.ExplorationField.TrySampleNoveltyDirection(
                transform.position,
                _velocity,
                settings.ExplorationProbeDistance,
                out Vector3 direction))
        {
            return Vector3.zero;
        }

        LastUsedExplorationBias = true;
        ExplorationBiasSteps++;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    Vector3 ComputeForagingBias(SwarmFlightSettings settings)
    {
        if (settings.ForagingField == null || settings.ForagingWeight <= 0f)
            return Vector3.zero;

        if (!settings.ForagingField.TrySampleForagingDirection(
                AgentIndex,
                transform.position,
                _velocity,
                out Vector3 direction))
        {
            return Vector3.zero;
        }

        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    Vector3 ComputeScentBias(SwarmFlightSettings settings)
    {
        if (settings.ScentField == null || settings.ScentWeight <= 0f)
            return Vector3.zero;

        if (!settings.ScentField.TrySampleScentDirection(
                AgentIndex,
                transform.position,
                _velocity,
                out Vector3 direction))
        {
            return Vector3.zero;
        }

        LastUsedScentBias = true;
        ScentBiasSteps++;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    Vector3 ComputeCoordinationBias(SwarmFlightSettings settings)
    {
        if (settings.CoordinationField == null || settings.CoordinationWeight <= 0f)
            return Vector3.zero;

        if (!settings.CoordinationField.TrySampleCoordinationDirection(
                AgentIndex,
                transform.position,
                _velocity,
                out Vector3 direction))
        {
            return Vector3.zero;
        }

        LastUsedCoordinationBias = true;
        CoordinationBiasSteps++;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    public void SetLastCoordinationFoodTarget(int foodSiteIndex)
    {
        LastCoordinationFoodTarget = foodSiteIndex;
    }

    static Vector3 ObstacleAvoidanceDirection(
        Vector3 position,
        SwarmFlightObstacle obstacle,
        out float surfaceDistance)
    {
        Vector3 offset = position - obstacle.Center;
        float absX = Mathf.Abs(offset.x);
        float absZ = Mathf.Abs(offset.z);
        float outsideX = Mathf.Max(absX - obstacle.HalfExtentsXZ.x, 0f);
        float outsideZ = Mathf.Max(absZ - obstacle.HalfExtentsXZ.y, 0f);

        if (outsideX > 0f || outsideZ > 0f)
        {
            Vector3 closest = new Vector3(
                obstacle.Center.x + Mathf.Clamp(offset.x, -obstacle.HalfExtentsXZ.x, obstacle.HalfExtentsXZ.x),
                position.y,
                obstacle.Center.z + Mathf.Clamp(offset.z, -obstacle.HalfExtentsXZ.y, obstacle.HalfExtentsXZ.y));
            Vector3 away = position - closest;
            away.y = 0f;
            surfaceDistance = Mathf.Sqrt(outsideX * outsideX + outsideZ * outsideZ);
            return away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
        }

        float penetrationX = obstacle.HalfExtentsXZ.x - absX;
        float penetrationZ = obstacle.HalfExtentsXZ.y - absZ;
        surfaceDistance = 0f;
        if (penetrationX < penetrationZ)
            return new Vector3(Mathf.Sign(offset.x == 0f ? 1f : offset.x), 0f, 0f);

        return new Vector3(0f, 0f, Mathf.Sign(offset.z == 0f ? 1f : offset.z));
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
