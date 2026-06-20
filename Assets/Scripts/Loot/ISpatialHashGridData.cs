using UnityEngine;

public interface ISpatialHashGridData
{
    Vector2 GetPosition();

    void SetCellKey(Vector2Int index);

    Vector2Int GetCellKey();
}
