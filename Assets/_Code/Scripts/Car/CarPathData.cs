using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static RoadSection;

public class CarPathData
{
    public QuadBezier2 curve;
    public int level;
    public RoadType type;

    public CarPathData(QuadBezier2 curve, int level, RoadType type)
    {
        this.curve = curve;
        this.level = level;
        this.type = type;
    }

    public static List<CarPathData> GetFromRoadSections(List<GraphNode> list, Gateway startGW, Gateway endGW)
    {
        List<CarPathData> res = new List<CarPathData>();
        float l = 1.7f;

        var startGwCurve = startGW.Curve;
        var starGwSplit = startGwCurve.SplitAt((startGwCurve.Length - l) / startGwCurve.Length);

        res.Add(new(starGwSplit.left, 0, RoadType.Flat));

        for (int i = 0; i < list.Count; i++)
        {
            var current = list[i].Road.Curve;
            var currentSplit1 = current.SplitAt(l / list[i].Road.Curve.Length);
            var currentSplit2 = currentSplit1.right.SplitAt((currentSplit1.right.Length - l) / currentSplit1.right.Length);

            QuadBezier2 prevCurve;

            if (i == 0)
            {
                prevCurve = startGW.Curve;
            }
            else
            {
                prevCurve = list[i - 1].Road.Curve;
            }

            var prevCurveSplit = prevCurve.SplitAt((prevCurve.Length - l) / prevCurve.Length);

            var firstDir = prevCurveSplit.right.GetTangent(0);
            var firstPoint = prevCurveSplit.right.GetPoint(0);

            var secondDir = -currentSplit1.left.GetTangent(1);
            var secondPoint = currentSplit1.left.GetPoint(1);

            if (Intersects(firstPoint, firstDir, secondPoint, secondDir, out Vector2 intersection))
            {
                QuadBezier2 newCurve = new(firstPoint, intersection, secondPoint);
                res.Add(new(newCurve, list[i].Road.Level, RoadType.Flat));
            }
            else
            {
                QuadBezier2 newCurve = new(firstPoint, secondPoint);
                res.Add(new(newCurve, list[i].Road.Level, RoadType.Flat));
            }

            res.Add(new(currentSplit2.left, list[i].Road.Level, list[i].Road.Type));
        }

        var lastCurve = list[list.Count - 1].Road.Curve;
        var lastCurveSplit = lastCurve.SplitAt((lastCurve.Length - l) / lastCurve.Length);

        var endGWCurve = endGW.Curve;
        var endGWSplit = endGWCurve.SplitAt((l) / endGWCurve.Length);

        var p0Dir = lastCurveSplit.right.GetTangent(0);
        var p0Pos = lastCurveSplit.right.GetPoint(0);

        var p1Dir = -endGWSplit.left.GetTangent(1);
        var p1Pos = endGWSplit.left.GetPoint(1);

        if (Intersects(p0Pos, p0Dir, p1Pos, p1Dir, out Vector2 inters))
        {
            QuadBezier2 newCurve = new(p0Pos, inters, p1Pos);
            res.Add(new(newCurve, 0, RoadType.Flat));
        }
        else
        {
            QuadBezier2 newCurve = new(p0Pos, p1Pos);
            res.Add(new(newCurve, 0, RoadType.Flat));
        }

        res.Add(new(endGWSplit.right, 0, RoadType.Flat));

        return res;
    }

    public static bool Intersects(Vector2 firstPoint, Vector2 firstDir, Vector2 secondPoint, Vector2 secondDir, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float cross = firstDir.x * secondDir.y - firstDir.y * secondDir.x;

        if (Mathf.Abs(cross) < 1e-6f)
            return false;

        Vector2 delta = secondPoint - firstPoint;

        float t = (delta.x * secondDir.y - delta.y * secondDir.x) / cross;
        float u = (delta.x * firstDir.y - delta.y * firstDir.x) / cross;

        if (t >= 0 && u >= 0)
        {
            intersection = firstPoint + t * firstDir;
            return true;
        }

        return false;
    }
}