using System.Collections.Generic;
using UnityEngine;

public class SpatialHashGrid
{
    private static SpatialHashGrid _instance;

    private Vector2 center;
    private Vector2 bounds;
    private Vector2Int dimensions;
    private float LeftEdge => center.x - bounds.x / 2;
    private float RightEdge => center.x + bounds.x / 2;
    private float BottomEdge => center.y - bounds.y / 2;
    private float TopEdge => center.y + bounds.y / 2;
    private Vector2 cellSize => new Vector2(bounds.x / dimensions.x, bounds.y / dimensions.y);

    private Dictionary<Vector2Int, HashSet<ISpatialHashGridData>> cells;

    public static SpatialHashGrid Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SpatialHashGrid(Vector3.zero, Vector2.one * 100, Vector2Int.one * 100);
            }
            return _instance;
        }
    }

    private SpatialHashGrid(Vector2 center, Vector2 bounds, Vector2Int dimensions)
    {
        this.center = center;
        this.bounds = bounds;
        this.dimensions = dimensions;
        cells = new Dictionary<Vector2Int, HashSet<ISpatialHashGridData>>();
    }



    public bool Insert(ISpatialHashGridData data)
    {
        Vector2Int cellKey = GetCellForPosition(data.GetPosition());
        data.SetCellKey(cellKey);
        //Debug.Log($"Inserted {data} at {cellKey}");
        if (!cells.ContainsKey(cellKey))
        {
            cells.Add(cellKey, new HashSet<ISpatialHashGridData>());
        }
        bool insertResult = cells[cellKey].Add(data);

        //Debug.Log($"Now {cells.GetValueOrDefault(cellKey, new()).Count} objects in cell {cellKey}");
        return insertResult;
    }

    public bool Remove(ISpatialHashGridData data)
    {
        return cells.GetValueOrDefault(data.GetCellKey(), new HashSet<ISpatialHashGridData>()).Remove(data);
    }

    public bool Update(ISpatialHashGridData data)
    {
        if (Remove(data))
        {
            return Insert(data);
        }
        return false;
    }

    public List<ISpatialHashGridData> GetNearObjects(Vector2 position, float radius)
    {
        List<ISpatialHashGridData> results = new();

        Vector2Int cell = GetCellForPosition(position);
        Vector2Int radiusInCells = new Vector2Int(Mathf.CeilToInt(radius / cellSize.x), Mathf.CeilToInt(radius / cellSize.y));
        for (int i = cell.x - radiusInCells.x; i <= cell.x + radiusInCells.x; i++)
        {
            for (int j = cell.y - radiusInCells.y; j <= cell.y + radiusInCells.y; j++)
            {
                Vector2Int cellKey = new Vector2Int(i, j);
                //Debug.Log($"Checking {cellKey}");
                //Debug.Log($"{cells.GetValueOrDefault(cellKey, new()).Count} objects in cell {cellKey}");
                foreach (ISpatialHashGridData data in cells.GetValueOrDefault(cellKey, new()))
                {
                    if (Vector2.Distance(data.GetPosition(), position) <= radius)
                    {
                        results.Add(data);
                    }
                }
            }
        }
        //Debug.Log($"Found {results.Count} results");
        return results;
    }

    private Vector2Int GetCellForPosition(Vector2 position)
    {
        int cellX = Mathf.Clamp((int)((position.x - LeftEdge) / cellSize.x), 0, dimensions.x - 1);
        int cellY = Mathf.Clamp((int)((position.y - BottomEdge) / cellSize.y), 0, dimensions.y - 1);
        return new Vector2Int(cellX, cellY);
    }
}
