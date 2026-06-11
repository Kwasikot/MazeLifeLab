using UnityEngine;

public class WallFollowerAgent : MonoBehaviour
{
    public enum HandRule
    {
        Right,
        Left
    }

    static readonly int[][] CardinalDirections =
    {
        new[] { 0, 1 },
        new[] { 1, 0 },
        new[] { 0, -1 },
        new[] { -1, 0 }
    };

    HandRule _handRule = HandRule.Right;
    MazeGenerator _generator;
    bool _enabled;

    int _cellX;
    int _cellY;
    int _facingX;
    int _facingZ;
    float _agentHeight;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int CollisionCount { get; private set; }

    public void BeginEpisode(HandRule handRule, MazeGenerator generator, float agentHeight)
    {
        _handRule = handRule;
        _generator = generator;
        _agentHeight = agentHeight;
        CollisionCount = 0;
        _enabled = true;

        if (!_generator.TryWorldToCell(transform.position, out _cellX, out _cellY))
        {
            _cellX = 0;
            _cellY = 0;
        }

        FaceFirstOpenPassage();
        SnapToGrid();

        Debug.Log(
            $"[WallFollower] start cell=({_cellX},{_cellY}) " +
            $"open={_generator.DescribeOpenPassages(_cellX, _cellY)} " +
            $"facing=({ _facingX},{_facingZ}) hand={handRule}");
    }

    public void EndEpisode()
    {
        _enabled = false;
        _generator = null;
    }

    public void ExecuteStep()
    {
        if (!_enabled || _generator == null)
            return;

        if (_handRule == HandRule.Right)
            ExecuteRightHandRule();
        else
            ExecuteLeftHandRule();
    }

    // EXP-001 doc §9.2
    void ExecuteRightHandRule()
    {
        GetRelativeDirections(out int rightX, out int rightZ, out int leftX, out int leftZ);

        if (IsPassageOpen(rightX, rightZ))
        {
            TurnRight();
            MoveForward();
            return;
        }

        if (IsPassageOpen(_facingX, _facingZ))
        {
            MoveForward();
            return;
        }

        if (IsPassageOpen(leftX, leftZ))
        {
            TurnLeft();
            return;
        }

        TurnAround();
    }

    void ExecuteLeftHandRule()
    {
        GetRelativeDirections(out int rightX, out int rightZ, out int leftX, out int leftZ);

        if (IsPassageOpen(leftX, leftZ))
        {
            TurnLeft();
            MoveForward();
            return;
        }

        if (IsPassageOpen(_facingX, _facingZ))
        {
            MoveForward();
            return;
        }

        if (IsPassageOpen(rightX, rightZ))
        {
            TurnRight();
            return;
        }

        TurnAround();
    }

    void FaceFirstOpenPassage()
    {
        foreach (int[] dir in CardinalDirections)
        {
            if (!_generator.IsPassageOpen(_cellX, _cellY, dir[0], dir[1]))
                continue;

            _facingX = dir[0];
            _facingZ = dir[1];
            return;
        }

        _facingX = 0;
        _facingZ = 1;
        Debug.LogWarning(
            $"[WallFollower] cell=({_cellX},{_cellY}) has no open passages — " +
            "regenerate maze or check start cell.");
    }

    void GetRelativeDirections(out int rightX, out int rightZ, out int leftX, out int leftZ)
    {
        rightX = _facingZ;
        rightZ = -_facingX;
        leftX = -_facingZ;
        leftZ = _facingX;
    }

    bool IsPassageOpen(int dirX, int dirZ)
    {
        return _generator.IsPassageOpen(_cellX, _cellY, dirX, dirZ);
    }

    void MoveForward()
    {
        if (!IsPassageOpen(_facingX, _facingZ))
        {
            CollisionCount++;
            return;
        }

        int nextX = _cellX + _facingX;
        int nextY = _cellY + _facingZ;
        if (!_generator.IsCellInBounds(nextX, nextY))
        {
            CollisionCount++;
            return;
        }

        _cellX = nextX;
        _cellY = nextY;
        SnapToGrid();
    }

    void TurnRight()
    {
        int nextFacingX = _facingZ;
        int nextFacingZ = -_facingX;
        _facingX = nextFacingX;
        _facingZ = nextFacingZ;
        SnapToGrid();
    }

    void TurnLeft()
    {
        int nextFacingX = -_facingZ;
        int nextFacingZ = _facingX;
        _facingX = nextFacingX;
        _facingZ = nextFacingZ;
        SnapToGrid();
    }

    void TurnAround()
    {
        _facingX = -_facingX;
        _facingZ = -_facingZ;
        SnapToGrid();
    }

    void SnapToGrid()
    {
        Vector3 center = _generator.GetCellCenterWorld(_cellX, _cellY);
        center.y = _agentHeight;
        transform.position = center;
        transform.rotation = Quaternion.LookRotation(new Vector3(_facingX, 0f, _facingZ));
    }
}
