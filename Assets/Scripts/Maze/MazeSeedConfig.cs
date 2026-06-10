using System;
using UnityEngine;

[Serializable]
public class MazeSeedConfig
{
    [Tooltip("Deterministic seed for maze carving. Same seed => same maze.")]
    public int mazeSeed = 42;

    public int mazeWidthCells = 20;
    public int mazeHeightCells = 20;
    public int cellSize = 5;
}
