using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uniform grid for proximity queries. Instances belong to a single lobby and live on its
/// <see cref="GameContext"/>; there is deliberately no static access any more, because a
/// shared grid mixed the enemies and loot of all concurrently running lobbies.
/// </summary>
public class SpatialHashGrid<T> where T : ISpatialHashGridData
{
    private Vector2 center;
    private Vector2 bounds;
    private Vector2Int dimensions;
    private float LeftEdge => center.x - bounds.x / 2;
    private float RightEdge => center.x + bounds.x / 2;
    private float BottomEdge => center.y - bounds.y / 2;
    private float TopEdge => center.y + bounds.y / 2;
    private Vector2 cellSize => new Vector2(bounds.x / dimensions.x, bounds.y / dimensions.y);

    private Dictionary<Vector2Int, HashSet<T>> cells;

    public SpatialHashGrid(Vector2 center, Vector2 bounds, Vector2Int dimensions)
    {
        this.center = center;
        this.bounds = bounds;
        this.dimensions = dimensions;
        cells = new Dictionary<Vector2Int, HashSet<T>>();
    }

    public bool Insert(T data)
    {
        Vector2Int cellKey = GetCellForPosition(data.GetPosition());
        data.SetCellKey(cellKey);
        if (!cells.ContainsKey(cellKey))
        {
            cells.Add(cellKey, new HashSet<T>());
        }
        return cells[cellKey].Add(data);
    }

    public bool Remove(T data)
    {
        return cells.GetValueOrDefault(data.GetCellKey(), new HashSet<T>()).Remove(data);
    }

    public bool Update(T data)
    {
        if (GetCellForPosition(data.GetPosition()) == data.GetCellKey())
        {
            return false;
        }
        if (Remove(data))
        {
            return Insert(data);
        }
        return false;
    }

    public List<T> GetNearObjects(Vector2 position, float radius)
    {
        List<T> results = new();

        Vector2Int cell = GetCellForPosition(position);
        Vector2Int radiusInCells = new Vector2Int(Mathf.CeilToInt(radius / cellSize.x), Mathf.CeilToInt(radius / cellSize.y));
        for (int i = cell.x - radiusInCells.x; i <= cell.x + radiusInCells.x; i++)
        {
            for (int j = cell.y - radiusInCells.y; j <= cell.y + radiusInCells.y; j++)
            {
                Vector2Int cellKey = new Vector2Int(i, j);
                foreach (T data in cells.GetValueOrDefault(cellKey, new()))
                {
                    if (Vector2.Distance(data.GetPosition(), position) <= radius)
                    {
                        results.Add(data);
                    }
                }
            }
        }
        return results;
    }

    private Vector2Int GetCellForPosition(Vector2 position)
    {
        int cellX = Mathf.Clamp((int)((position.x - LeftEdge) / cellSize.x), 0, dimensions.x - 1);
        int cellY = Mathf.Clamp((int)((position.y - BottomEdge) / cellSize.y), 0, dimensions.y - 1);
        return new Vector2Int(cellX, cellY);
    }
}
