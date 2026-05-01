using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameRules
{

    public static readonly int MaxLevel = 2;
    public static readonly int LevelHeight = 4;
    public static readonly float MinBranchAngle = 20f;

    public static readonly float MinSlopeLength = 20f;
    public static readonly float MinStraightLength = 5f;

    public static readonly float SnappingVecThreshold = 2f;
    public static readonly float SnappingAngleThreshold = 5f;

    public static readonly float CarAccelerate = 5f;
    public static readonly float CarDeccelerate = 7;

    public static float UpFunction(float x)
    {
        float flatOffset = 0.08f;
        if (x < flatOffset)
        {
            return 0;
        }
        else if (x < (1 - flatOffset))
        {
            return LinearQuadEasInOut(x / (1 - flatOffset * 2) - flatOffset / (1 - flatOffset * 2), 0.22f);
        }
        else return 1;
    }

    public static float UpFunctionDerivateive(float x)
    {
        float flatOffset = 0.08f;
        if (x < flatOffset)
        {
            return 0;
        }
        else if (x < (1 - flatOffset))
        {
            return LinearQuadEasInOutDerivative(x / (1 - flatOffset * 2) - flatOffset / (1 - flatOffset * 2), 0.22f);
        }
        else return 0;
    }
    private static float LinearQuadEasInOut(float x, float k = 0.25f)
    {
        x = Mathf.Clamp01(x);
        float v = 1 / (1 - k);

        if (x < k)
        {
            return (v / (2f * k)) * x * x;
        }
        else if (x < 1 - k)
        {
            return v * (x - k / 2f);
        }
        else
        {
            float tInv = 1f - x;
            return 1f - (v / (2f * k)) * tInv * tInv;
        }
    }

    private static float LinearQuadEasInOutDerivative(float x, float k = 0.25f)
    {
        x = Mathf.Clamp01(x);
        float v = 1f / (1f - k);

        if (x < k)
        {
            return (v / k) * x;
        }
        else if (x < 1f - k)
        {
            return v;
        }
        else
        {
            float tInv = 1f - x;
            return (v / k) * tInv;
        }
    }

    public static float MinAngleFlat(float anchorDistance) => Mathf.Max(30, -anchorDistance * 1.25f / 2 + 75);
    public static float MinAngleSlope(float anchorDistance) => Mathf.Max(45, -anchorDistance * 2.25f / 2 + 112);

    public static float CurvatureToMaxSpeed(float curvature) => Mathf.Clamp(-15.47f * curvature + 8.77f, 2.1f, 13);

}
