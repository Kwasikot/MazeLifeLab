using System;
using UnityEngine;

[Serializable]
public struct MazeCellIndex
{
    public int x;
    public int y;

    public MazeCellIndex(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString() => $"({x}, {y})";
}
