using UnityEngine;

public static class VectorExtensions
{
    public static Vector3 ToV3(this Vector2 vec)
    {
        return new Vector3(vec.x, vec.y, 0);
    }
    public static Vector2 ToV2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
}