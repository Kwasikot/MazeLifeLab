using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class Experiment001CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool useOrthographic = true;
    [SerializeField] float height = 80f;
    [SerializeField] float minHeight = 5f;
    [SerializeField] float maxHeight = 5000f;
    [SerializeField] float orthographicSize = 60f;
    [SerializeField] float minOrthographicSize = 4f;
    [SerializeField] float maxOrthographicSize = 10000f;
    [SerializeField] float zoomSpeed = 8f;
    [SerializeField] float followSmoothing = 12f;
    [SerializeField] KeyCode viewFullMazeKey = KeyCode.F;

    Vector3 _mazeCenter = new Vector3(50f, 0f, 50f);
    float _fullViewOrthographicSize;
    bool _viewFullMaze = true;
    bool _followTeam;
    Transform[] _teamTargets;
    float _teamPaddingCells = 5f;
    int _teamCellSize = 5;
    bool _snapNextFrame;

    Camera _camera;

    public bool IsFullMazeView => _viewFullMaze;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float Height
    {
        get => height;
        set => height = Mathf.Clamp(value, minHeight, maxHeight);
    }

    public static void TryConfigureMainCamera(MazeGen mazeGen, bool showFullMaze = true)
    {
        if (mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<Experiment001CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<Experiment001CameraFollow>();

        follow.ConfigureForMaze(
            mazeGen.Config.mazeWidthCells,
            mazeGen.Config.mazeHeightCells,
            mazeGen.Config.cellSize);

        if (showFullMaze)
            follow.ShowFullMazeView(snapImmediately: true);
    }

    public void ConfigureForMaze(int widthCells, int heightCells, int cellSize)
    {
        EnsureCamera();

        float mazeWidth = widthCells * cellSize;
        float mazeHeight = heightCells * cellSize;
        _mazeCenter = new Vector3(mazeWidth * 0.5f, 0f, mazeHeight * 0.5f);

        float padding = cellSize * 2f;
        float halfHeight = mazeHeight * 0.5f + padding;
        float halfWidth = mazeWidth * 0.5f + padding;
        float aspect = GetViewportAspect();

        float sizeForHeight = halfHeight;
        float sizeForWidth = halfWidth / aspect;
        float fullMazeSize = Mathf.Max(sizeForHeight, sizeForWidth);

        _fullViewOrthographicSize = fullMazeSize;
        orthographicSize = fullMazeSize;
        maxOrthographicSize = fullMazeSize * 1.25f;
        minOrthographicSize = Mathf.Min(4f, fullMazeSize * 0.05f);

        float halfExtent = Mathf.Max(mazeWidth, mazeHeight) * 0.5f;
        height = Mathf.Max(80f, halfExtent * 0.12f);
        minHeight = 5f;
        maxHeight = Mathf.Max(height * 4f, halfExtent);

        if (_camera != null)
        {
            _camera.farClipPlane = height + halfExtent * 2f + 500f;
            _camera.nearClipPlane = 0.3f;
        }
    }

    public void ShowFullMazeView(bool snapImmediately = false)
    {
        _viewFullMaze = true;
        _followTeam = false;
        orthographicSize = _fullViewOrthographicSize > 0f ? _fullViewOrthographicSize : maxOrthographicSize;
        ApplyCameraProjection();

        if (snapImmediately)
            SnapToFocus();
        else
            _snapNextFrame = true;
    }

    public void FollowAgent(bool snapImmediately = false)
    {
        _viewFullMaze = false;
        _followTeam = false;
        orthographicSize = Mathf.Clamp(orthographicSize, minOrthographicSize, maxOrthographicSize * 0.45f);
        ApplyCameraProjection();

        if (snapImmediately)
            SnapToFocus();
        else
            _snapNextFrame = true;
    }

    public void FollowTeam(IReadOnlyList<Transform> agents, int cellSize, bool snapImmediately = false)
    {
        _viewFullMaze = false;
        _followTeam = true;
        _teamCellSize = Mathf.Max(1, cellSize);

        if (agents == null || agents.Count == 0)
        {
            _teamTargets = null;
            return;
        }

        _teamTargets = new Transform[agents.Count];
        for (int i = 0; i < agents.Count; i++)
            _teamTargets[i] = agents[i];

        UpdateTeamFraming();
        ApplyCameraProjection();

        if (snapImmediately)
            SnapToFocus();
        else
            _snapNextFrame = true;
    }

    void Awake()
    {
        EnsureCamera();
        ApplyCameraProjection();
    }

    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (useOrthographic && _camera != null)
            {
                orthographicSize = Mathf.Clamp(
                    orthographicSize - scroll * zoomSpeed,
                    minOrthographicSize,
                    maxOrthographicSize);
                _camera.orthographicSize = orthographicSize;
            }
            else
            {
                height = Mathf.Clamp(height - scroll * zoomSpeed, minHeight, maxHeight);
            }
        }

        if (Input.GetKeyDown(viewFullMazeKey))
        {
            if (_viewFullMaze)
            {
                if (_teamTargets != null && _teamTargets.Length > 0)
                    FollowTeam(_teamTargets, _teamCellSize, snapImmediately: true);
                else if (target != null)
                    FollowAgent(snapImmediately: true);
            }
            else
            {
                ShowFullMazeView(snapImmediately: true);
            }
        }
    }

    void LateUpdate()
    {
        if (_camera != null && useOrthographic)
            _camera.orthographicSize = orthographicSize;

        if (_followTeam)
            UpdateTeamFraming();

        Vector3 focus = GetFocusPoint();
        Vector3 desiredPosition = focus + Vector3.up * height;

        if (_snapNextFrame)
        {
            ApplyViewState();
            _snapNextFrame = false;
        }
        else if (useOrthographic)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSmoothing * Time.deltaTime);
        }

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    float GetViewportAspect()
    {
        if (_camera != null && _camera.pixelHeight > 0)
            return (float)_camera.pixelWidth / _camera.pixelHeight;

        return 16f / 9f;
    }

    Vector3 GetFocusPoint()
    {
        if (_viewFullMaze)
            return _mazeCenter;

        if (_followTeam && _teamTargets != null && _teamTargets.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _teamTargets.Length; i++)
            {
                if (_teamTargets[i] == null)
                    continue;

                sum += _teamTargets[i].position;
                count++;
            }

            if (count > 0)
                return sum / count;
        }

        if (target != null)
            return target.position;

        return _mazeCenter;
    }

    void UpdateTeamFraming()
    {
        if (_teamTargets == null || _teamTargets.Length == 0)
            return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;
        int count = 0;

        for (int i = 0; i < _teamTargets.Length; i++)
        {
            Transform t = _teamTargets[i];
            if (t == null)
                continue;

            Vector3 p = t.position;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);
            count++;
        }

        if (count == 0)
            return;

        float padding = _teamPaddingCells * _teamCellSize;
        float halfHeight = (maxZ - minZ) * 0.5f + padding;
        float halfWidth = (maxX - minX) * 0.5f + padding;
        float aspect = GetViewportAspect();
        float sizeForHeight = halfHeight;
        float sizeForWidth = halfWidth / aspect;
        orthographicSize = Mathf.Clamp(
            Mathf.Max(sizeForHeight, sizeForWidth, minOrthographicSize * 2f),
            minOrthographicSize,
            maxOrthographicSize);
    }

    void SnapToFocus()
    {
        ApplyViewState();
        _snapNextFrame = false;
    }

    void ApplyViewState()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        transform.position = GetFocusPoint() + Vector3.up * height;
        ApplyCameraProjection();
    }

    void ApplyCameraProjection()
    {
        EnsureCamera();

        if (_camera == null || !useOrthographic)
            return;

        _camera.orthographic = true;
        _camera.orthographicSize = orthographicSize;
    }

    void EnsureCamera()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
    }
}
