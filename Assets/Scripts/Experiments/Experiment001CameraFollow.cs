using UnityEngine;

public class Experiment001CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool useOrthographic = true;
    [SerializeField] float height = 80f;
    [SerializeField] float minHeight = 5f;
    [SerializeField] float maxHeight = 300f;
    [SerializeField] float orthographicSize = 60f;
    [SerializeField] float minOrthographicSize = 4f;
    [SerializeField] float maxOrthographicSize = 90f;
    [SerializeField] float zoomSpeed = 8f;
    [SerializeField] float followSmoothing = 12f;
    [SerializeField] KeyCode viewFullMazeKey = KeyCode.F;

    Vector3 _mazeCenter = new Vector3(50f, 0f, 50f);
    bool _viewFullMaze = true;
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

    public void ConfigureForMaze(int widthCells, int heightCells, int cellSize)
    {
        float mazeWidth = widthCells * cellSize;
        float mazeHeight = heightCells * cellSize;
        _mazeCenter = new Vector3(mazeWidth * 0.5f, 0f, mazeHeight * 0.5f);

        float halfExtent = Mathf.Max(mazeWidth, mazeHeight) * 0.5f;
        orthographicSize = halfExtent + cellSize * 2f;
        maxOrthographicSize = halfExtent + cellSize * 4f;
    }

    public void ShowFullMazeView(bool snapImmediately = false)
    {
        _viewFullMaze = true;
        orthographicSize = maxOrthographicSize;

        if (_camera != null && useOrthographic)
            _camera.orthographicSize = orthographicSize;

        if (snapImmediately)
            SnapToFocus();
        else
            _snapNextFrame = true;
    }

    public void FollowAgent(bool snapImmediately = false)
    {
        _viewFullMaze = false;
        orthographicSize = Mathf.Clamp(orthographicSize, minOrthographicSize, maxOrthographicSize * 0.45f);

        if (_camera != null && useOrthographic)
            _camera.orthographicSize = orthographicSize;

        if (snapImmediately)
            SnapToFocus();
        else
            _snapNextFrame = true;
    }

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera != null && useOrthographic)
            _camera.orthographic = true;
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
                FollowAgent(snapImmediately: true);
            else
                ShowFullMazeView(snapImmediately: true);
        }
    }

    void LateUpdate()
    {
        if (_camera != null && useOrthographic)
            _camera.orthographicSize = orthographicSize;

        Vector3 focus = GetFocusPoint();
        Vector3 desiredPosition = focus + Vector3.up * height;

        if (_snapNextFrame)
        {
            transform.position = desiredPosition;
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

    Vector3 GetFocusPoint()
    {
        if (_viewFullMaze || target == null)
            return _mazeCenter;

        return target.position;
    }

    void SnapToFocus()
    {
        transform.position = GetFocusPoint() + Vector3.up * height;
        _snapNextFrame = false;
    }
}
