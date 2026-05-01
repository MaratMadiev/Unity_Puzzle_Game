using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

public class QuadBezier2
{
    Vector2 pointA;
    Vector2 pointB;
    Vector2 pointC;

    float length;

    public Vector2 PointA
    {
        get { return pointA; }
    }

    public Vector2 PointB
    {
        get { return pointB; }
    }

    public Vector2 PointC
    {
        get { return pointC; }
    }

    public float Length
    {
        get
        {
            return length;
        }
    }

    public QuadBezier2(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        pointA = p0;
        pointB = p1;
        pointC = p2;

        length = CalculateLength();
    }

    public QuadBezier2(Vector2 p0, Vector2 p1)
    {
        pointA = p0;
        pointB = (p0 + p1) / 2;
        pointC = p1;

        length = CalculateLength();
    }

    public Vector2 GetPoint(float t)
    {
        if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t), "t must be between 0 and 1");

        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector2 point = uu * pointA + 2 * u * t * pointB + tt * pointC;
        return point;
    }

    public Vector2 GetPointLen(float l)
    {
        float t = l / length;
        return GetPoint(t);
    }

    public List<Vector2> GetLerpPoints(int segNum)
    {
        if (segNum <= 0) throw new ArgumentOutOfRangeException(nameof(segNum), "segNum must be more then 0");
        List<Vector2> ret = new List<Vector2> { pointA };

        for (int i = 1; i < segNum; i++)
        {
            var point = GetPoint(i * 1f / segNum);
            ret.Add(point);
        }
        ret.Add(pointC);

        return ret;
    }
    public List<Vector2> GetLerpPointsLen(float segmentLength)
    {
        int segNum = (int)(length / segmentLength);
        return GetLerpPoints(segNum < 1 ? 1 : segNum);
    }

    public Vector2 GetTangent(float t)
    {
        if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t), "t must be between 0 and 1");

        float u = 1 - t;

        Vector2 tangent = 2 * (u * (pointB - pointA) + t * (pointC - pointB));

        if (tangent.magnitude > 0.0001f)
            tangent = tangent.normalized;

        return tangent;
    }

    public float CalculateLength()
    {
        return AdaptiveSimpsonIntegration(0, 1, 0.0001f);
    }

    private float AdaptiveSimpsonIntegration(float a, float b, float epsilon, int maxDepth = 10)
    {
        float m = (a + b) / 2;
        float fa = GetDerivativeMagnitude(a);
        float fm = GetDerivativeMagnitude(m);
        float fb = GetDerivativeMagnitude(b);

        float whole = (b - a) * (fa + 4 * fm + fb) / 6;
        float left = (m - a) * (fa + 4 * GetDerivativeMagnitude((a + m) / 2) + fm) / 6;
        float right = (b - m) * (fm + 4 * GetDerivativeMagnitude((m + b) / 2) + fb) / 6;

        if (Mathf.Abs(left + right - whole) < 15 * epsilon || maxDepth <= 0)
            return left + right;

        return AdaptiveSimpsonIntegration(a, m, epsilon / 2, maxDepth - 1) +
               AdaptiveSimpsonIntegration(m, b, epsilon / 2, maxDepth - 1);
    }

    private float GetDerivativeMagnitude(float t)
    {
        float u = 1 - t;
        Vector2 derivative = 2 * (u * (pointB - pointA) + t * (pointC - pointB));
        return derivative.magnitude;
    }

    public float GetCurvature(float t)
    {
        if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t), "t must be between 0 and 1");

        float u = 1 - t;

        // First derivative: B'(t) = 2((1-t)(B-A) + t(C-B))
        Vector2 firstDerivative = 2 * (u * (pointB - pointA) + t * (pointC - pointB));

        // Second derivative: B''(t) = 2((C-B) - (B-A)) = 2(C - 2B + A)
        Vector2 secondDerivative = 2 * (pointC - 2 * pointB + pointA);

        float crossProduct = Mathf.Abs(firstDerivative.x * secondDerivative.y - firstDerivative.y * secondDerivative.x);
        float denominator = Mathf.Pow(firstDerivative.magnitude, 3);

        if (denominator < 0.0001f)
            return 0;

        return crossProduct / denominator;
    }

    public (QuadBezier2 left, QuadBezier2 right) SplitAt(float t)
    {
        if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t), "t must be between 0 and 1");

        Vector2 p01 = Vector2.Lerp(pointA, pointB, t);
        Vector2 p12 = Vector2.Lerp(pointB, pointC, t);
        Vector2 p012 = Vector2.Lerp(p01, p12, t);

        QuadBezier2 left = new QuadBezier2(pointA, p01, p012);
        QuadBezier2 right = new QuadBezier2(p012, p12, pointC);

        return (left, right);
    }

    public Vector2 GetDerivative(float t)
    {
        if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t), "t must be between 0 and 1");

        float u = 1 - t;
        return 2 * (u * (pointB - pointA) + t * (pointC - pointB));
    }

    public Vector2 GetSecondDerivative()
    {
        return 2 * (pointC - 2 * pointB + pointA);
    }

    public Rect GetBoundingBox()
    {
        float minX = Mathf.Min(pointA.x, pointB.x, pointC.x);
        float minY = Mathf.Min(pointA.y, pointB.y, pointC.y);
        float maxX = Mathf.Max(pointA.x, pointB.x, pointC.x);
        float maxY = Mathf.Max(pointA.y, pointB.y, pointC.y);

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    public List<(float t, float s, Vector2 point)> GetIntersectionPoints(QuadBezier2 other, float tolerance = 0.01f)
    {
        var intersections = new List<(float t, float s, Vector2 point)>();
        FindIntersectionsRecursive(this, other, 0, 1, 0, 1, tolerance, intersections);
        return intersections;
    }

    private void FindIntersectionsRecursive(
        QuadBezier2 curve1, QuadBezier2 curve2,
        float t0, float t1, float s0, float s1,
        float tolerance, List<(float t, float s, Vector2 point)> results, int depth = 0)
    {
        if (depth > 20) return;

        Rect bounds1 = curve1.GetBoundingBox();
        Rect bounds2 = curve2.GetBoundingBox();

        if (!bounds1.Overlaps(bounds2))
            return;

        float tMid = (t0 + t1) / 2;
        float sMid = (s0 + s1) / 2;

        //if (Mathf.Abs(tMid) < 0.001 || Mathf.Abs(tMid) < 0.001)

        // Если оба прямоугольника достаточно маленькие - нашли пересечение
        if (bounds1.width < tolerance && bounds1.height < tolerance &&
            bounds2.width < tolerance && bounds2.height < tolerance)
        {
            float nearZeroTolerance = 0.001f;
            bool isAtStartOrEnd = (Mathf.Abs(tMid) < nearZeroTolerance * 2) ||
                       (Mathf.Abs(tMid - 1) < nearZeroTolerance * 2) ||
                       (Mathf.Abs(sMid) < nearZeroTolerance * 2) ||
                       (Mathf.Abs(sMid - 1) < nearZeroTolerance * 2);

            if (isAtStartOrEnd) return;

            Vector2 point = (curve1.GetPoint(0.5f) + curve2.GetPoint(0.5f)) / 2;

            bool duplicate = false;
            foreach (var existing in results)
            {
                if (Vector2.Distance(existing.point, point) < tolerance)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                results.Add((tMid, sMid, point));

            return;
        }

        var (left1, right1) = curve1.SplitAt(0.5f);
        var (left2, right2) = curve2.SplitAt(0.5f);

        FindIntersectionsRecursive(left1, left2, t0, tMid, s0, sMid, tolerance, results, depth + 1);
        FindIntersectionsRecursive(left1, right2, t0, tMid, sMid, s1, tolerance, results, depth + 1);
        FindIntersectionsRecursive(right1, left2, tMid, t1, s0, sMid, tolerance, results, depth + 1);
        FindIntersectionsRecursive(right1, right2, tMid, t1, sMid, s1, tolerance, results, depth + 1);
    }

    public override string ToString()
    {
        return $"QuadBezier2: P0={pointA}, P1={pointB}, P2={pointC}";
    }

    public bool IntersectsRect(Rect rect, int subdivisions = 10)
    {
        // Проверяем bounding box кривой
        Rect bounds = GetBoundingBox();
        if (!bounds.Overlaps(rect))
            return false;

        // Дискретно проверяем точки на кривой
        for (int i = 0; i <= subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            Vector2 point = GetPoint(t);

            if (rect.Contains(point))
                return true;
        }

        // Дополнительно проверяем пересечения с отрезками, образующими кривую
        Vector2 prevPoint = GetPoint(0);
        for (int i = 1; i <= subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            Vector2 currentPoint = GetPoint(t);

            if (LineIntersectsRect(prevPoint, currentPoint, rect))
                return true;

            prevPoint = currentPoint;
        }

        return false;
    }

    private bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rect rect)
    {
        Vector2[] rectCorners = new Vector2[]
        {
        new Vector2(rect.xMin, rect.yMin),
        new Vector2(rect.xMax, rect.yMin),
        new Vector2(rect.xMax, rect.yMax),
        new Vector2(rect.xMin, rect.yMax)
        };

        for (int i = 0; i < 4; i++)
        {
            Vector2 r1 = rectCorners[i];
            Vector2 r2 = rectCorners[(i + 1) % 4];

            if (LineSegmentsIntersect(p1, p2, r1, r2))
                return true;
        }

        if (rect.Contains(p1) || rect.Contains(p2))
            return true;

        return false;
    }

    private bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float orientation1 = Orientation(a1, a2, b1);
        float orientation2 = Orientation(a1, a2, b2);
        float orientation3 = Orientation(b1, b2, a1);
        float orientation4 = Orientation(b1, b2, a2);

        return (orientation1 * orientation2 < 0) && (orientation3 * orientation4 < 0);
    }

    private float Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        return Mathf.Sign(val);
    }
}