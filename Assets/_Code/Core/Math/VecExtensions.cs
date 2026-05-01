using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtensions
{
    public static Vector3 ToVector3XZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }

    public static Vector3 ToVector3XY(this Vector2 v, float z = 0)
    {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector2 ToVector2FromXZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        return new Vector2(
            (cos * v.x) - (sin * v.y),
            (sin * v.x) + (cos * v.y)
        );
    }
}
